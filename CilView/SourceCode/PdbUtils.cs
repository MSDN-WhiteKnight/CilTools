/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilView.Common;
using Internal.Pdb.Windows;

namespace CilView.SourceCode
{
    class PdbUtils
    {
        public static string GetCilText(MethodBase mb, uint startOffset, uint endOffset) 
        {
            StringBuilder sb = new StringBuilder(500);

            foreach (CilInstruction instr in CilReader.GetInstructions(mb)) 
            {
                if (instr.ByteOffset >= startOffset &&
                    instr.ByteOffset + instr.TotalSize <= endOffset)
                {
                    sb.AppendLine(instr.ToString());
                }
                else if (instr.ByteOffset > endOffset)
                {
                    break;
                }
            }

            return sb.ToString();
        }

        public static string GetSourceFromPdb<T>(Predicate<T> match) 
        {
            return GetSourceFromPdb(match.Method,0,uint.MaxValue,SymbolsQueryType.RangeExact).SourceCode;
        }

        /// <summary>
        /// Gets the source code for the specified bytecode fragment using PDB symbols
        /// </summary>
        /// <param name="m">Method for which to get the source code</param>
        /// <param name="startOffset">Byte offset of the bytecode fragment's start</param>
        /// <param name="endOffset">Byte offset of the bytecode fragment's end</param>
        /// <param name="queryType"> Specifies how to determine the source bytecode fragment:  
        /// <see cref="SymbolsQueryType.RangeExact"/> to return the sources for exact specified bytecode fragment; 
        /// <see cref="SymbolsQueryType.SequencePoint"/> to get the sources 
        /// for the sequence point in which starting offset lies.
        /// </param>
        public static SourceInfo GetSourceFromPdb(MethodBase m,
            uint startOffset,
            uint endOffset,
            SymbolsQueryType queryType)
        {
            int token = m.MetadataToken;

            //построим путь к файлу символов
            string module_path = m.DeclaringType.Assembly.Location;
            if(string.IsNullOrEmpty(module_path)) return SourceInfo.Empty;

            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            if (!File.Exists(pdb_path)) 
            {
                throw new SymbolsException("Symbols file not found: "+pdb_path);
            }

            StringBuilder sb = new StringBuilder();
            PdbReader reader = new PdbReader(pdb_path);
            SourceInfo ret = new SourceInfo();
            ret.SymbolsFile = pdb_path;
            ret.Method = m;
            ret.CilStart = startOffset;
            ret.CilEnd = endOffset;

            using (reader)
            {
                //найдем метод в символах
                var func = reader.GetFunctionFromToken((uint)token);

                if (func==null) return SourceInfo.Empty;
                if (func.SequencePoints==null) return SourceInfo.Empty;

                foreach (PdbSequencePointCollection coll in func.SequencePoints)
                {
                    if (coll.File == null) continue;
                    if (string.IsNullOrEmpty(coll.File.Name)) continue;

                    if (!File.Exists(coll.File.Name))
                    {
                        throw new SymbolsException("Source file not found: "+coll.File.Name);
                    }

                    //считываем файл исходников
                    string[] lines = File.ReadAllLines(coll.File.Name, Encoding.UTF8);

                    //найдем номера строк в файле, соответствующие началу и концу фрагмента
                    PdbSequencePoint start;
                    PdbSequencePoint end;

                    if (queryType == SymbolsQueryType.RangeExact)
                    {
                        var points_sorted = coll.Lines.
                            Where((x) => x.LineBegin <= lines.Length && x.LineEnd <= lines.Length &&
                            x.Offset >= startOffset && x.Offset < endOffset).
                            OrderBy((x) => x.Offset);

                        if (points_sorted.Count() == 0)
                        {
                            return SourceInfo.Empty;
                        }

                        start = points_sorted.First();
                        end = points_sorted.Last();
                    }
                    else if (queryType == SymbolsQueryType.SequencePoint)
                    {
                        PdbSequencePoint[] points=coll.Lines.
                            Where((x) => x.LineBegin <= lines.Length && x.LineEnd <= lines.Length).
                            OrderBy((x) => x.Offset).ToArray();

                        if(pdb_path.Length==0) return SourceInfo.Empty;

                        PdbSequencePoint p=points[0];
                        int p_index = 0;

                        for (int i = 1; i < points.Length; i++) 
                        {
                            if (points[i].Offset > startOffset) break;

                            p = points[i];
                            p_index = i;
                        }

                        if(p.Offset > startOffset) return SourceInfo.Empty;

                        start = p;
                        end = p;
                        ret.CilStart = p.Offset;

                        int pNext_index = p_index + 1;

                        if (pNext_index < points.Length) 
                        {
                            PdbSequencePoint pNext = points[pNext_index];
                            ret.CilEnd = pNext.Offset;
                        }
                        else
                        {
                            int bodySize = Utils.GetMethodBodySize(m);
                            if (bodySize > 0 && bodySize >= ret.CilStart) ret.CilEnd = (uint)bodySize;
                        }
                    }
                    else throw new NotSupportedException("Unknown symbols query type: "+queryType.ToString());

                    ret.SourceFile = coll.File.Name;
                    ret.LineStart = (int)start.LineBegin;
                    ret.LineEnd = (int)end.LineEnd;

                    bool reading = false;
                    int index_start;
                    int index_end;

                    //считаем код метода из исходников
                    for (int i = 1; i <= lines.Length; i++)
                    {
                        string line = lines[i - 1];
                        index_start = 0;
                        index_end = line.Length;

                        if (!reading)
                        {
                            if (i >= start.LineBegin)
                            {
                                //первая строка
                                reading = true;

                                if (startOffset != 0 || endOffset != uint.MaxValue)
                                {
                                    index_start = start.ColBegin - 1;
                                    if (index_start < 0) index_start = 0;
                                }
                                else 
                                {
                                    index_start = 0;
                                }
                            }
                        }

                        if (reading)
                        {
                            if (i >= end.LineEnd)
                            {
                                //последняя строка
                                index_end = end.ColEnd - 1;
                                if (index_end > line.Length) index_end = line.Length;

                                sb.AppendLine(line.Substring(index_start, index_end - index_start));
                                break;
                            }

                            //считывание текущей строки
                            sb.AppendLine(line.Substring(index_start, index_end - index_start));
                        }
                    }
                }//end foreach
            }//end using

            ret.SourceCode = sb.ToString();
            return ret;
        }
      
        public static string GetMethodSigString(MethodBase m)
        {
            StringBuilder sb = new StringBuilder(500);
            ParameterInfo[] pars = m.GetParameters();

            if (m.IsPublic) sb.Append("public ");
            else if(m.IsFamily) sb.Append("protected ");
            else if (m.IsAssembly) sb.Append("internal ");

            if(m.IsStatic) sb.Append("static ");

            string rettype = string.Empty;

            if (m is CustomMethod)
            {
                CustomMethod cm = (CustomMethod)m;
                Type t = cm.ReturnType;

                if (t != null)
                {
                    if (Utils.StringEquals(t.FullName, "System.Void"))
                    {
                        rettype = "void";
                    }
                    else
                    {
                        rettype = t.Name;
                    }
                }
            }

            sb.Append(rettype);
            sb.Append(' ');
            sb.Append(m.Name);

            if (m.IsGenericMethod)
            {
                sb.Append('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append('>');
            }

            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");
                sb.Append(pars[i].ParameterType.Name);
                sb.Append(' ');

                string parname = pars[i].Name;

                if (string.IsNullOrEmpty(parname))
                {
                    parname = "par" + (i + 1).ToString();
                }

                sb.Append(parname);
            }

            sb.Append(')');
            return sb.ToString();
        }

        /// <summary>
        /// Removes redundant indentation from the start of each line in the specified string
        /// </summary>
        public static string Deindent(string code)
        {
            string code_normalized = code.Replace("\r\n", "\n");
            char[] separators = new char[] { '\n' };
            string[] lines = code_normalized.Split(separators);

            //don't bother with single-line strings
            if (lines.Length <= 1) return code;

            //find common whitepace prefix length
            int minWhitespace = int.MaxValue;
            int cWhitespace;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;

                cWhitespace = 0;

                for (int j = 0; j < lines[i].Length; j++)
                {
                    if (lines[i][j] == ' ') cWhitespace++;
                    else break;
                }

                if (cWhitespace < minWhitespace)
                {
                    minWhitespace = cWhitespace;
                }
            }

            if (minWhitespace <= 0) return code; //no common whitespace prefix

            string[] lines_new = new string[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                //string whitespace off the start of each line
                string line = lines[i];
                int index = minWhitespace;
                if (index >= line.Length) index=0;
                line = line.Substring(index);
                lines_new[i] = line;
            }

            return string.Join(Environment.NewLine, lines_new);
        }
    }
}
