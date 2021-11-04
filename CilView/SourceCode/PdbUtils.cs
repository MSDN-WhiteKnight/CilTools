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
using Internal.Pdb.Portable;
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

        const uint PDB_HIDDEN_SEQUENCE_POINT = 0x00feefee;

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
            SourceInfo ret;

            try
            {
                //try Portable PDB first
                ret = GetSourceFromPortablePdb(m, startOffset, endOffset, queryType);
            }
            catch (BadImageFormatException)
            {
                ret = SourceInfo.FromError(SourceInfoError.InvalidFormat);
            }

            if (ret.Error == SourceInfoError.InvalidFormat)
            {
                //read Windows PDB if failed
                ret = GetSourceFromWindowsPdb(m, startOffset, endOffset, queryType);
            }

            return ret;
        }
        
        static SourceInfo GetSourceFromWindowsPdb(MethodBase m,
            uint startOffset,
            uint endOffset,
            SymbolsQueryType queryType)
        {
            int token = m.MetadataToken;

            //построим путь к файлу символов
            string module_path = m.DeclaringType.Assembly.Location;

            if (string.IsNullOrEmpty(module_path))
            {
                return SourceInfo.FromError(SourceInfoError.NoModulePath);
            }

            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            if (!File.Exists(pdb_path)) 
            {
                throw new SymbolsException("Symbols file not found: "+pdb_path);
            }

            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);
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
                if (func.SequencePoints==null) return SourceInfo.FromError(SourceInfoError.NoMatches);

                foreach (PdbSequencePointCollection coll in func.SequencePoints)
                {
                    if (coll.File == null) continue;
                    if (string.IsNullOrEmpty(coll.File.Name)) continue;

                    if (!File.Exists(coll.File.Name))
                    {
                        throw new SymbolsException("Source file not found: "+coll.File.Name);
                    }
                    
                    //найдем номера строк в файле, соответствующие началу и концу фрагмента
                    PdbSequencePoint start;
                    PdbSequencePoint end;
                    PdbSequencePoint pNext=new PdbSequencePoint();
                    bool has_pNext=false;

                    if (queryType == SymbolsQueryType.RangeExact)
                    {
                        var points_sorted = coll.Lines.
                            Where((x) => x.Offset >= startOffset && x.Offset < endOffset && 
                             x.LineBegin < PDB_HIDDEN_SEQUENCE_POINT &&
                             x.LineEnd < PDB_HIDDEN_SEQUENCE_POINT).
                            OrderBy((x) => x.Offset);

                        if (points_sorted.Count() == 0)
                        {
                            return SourceInfo.FromError(SourceInfoError.NoMatches);
                        }

                        start = points_sorted.First();
                        end = points_sorted.Last();
                        has_pNext = false;
                    }
                    else if (queryType == SymbolsQueryType.SequencePoint)
                    {
                        PdbSequencePoint[] points = coll.Lines.
                            Where((x) => x.LineBegin < PDB_HIDDEN_SEQUENCE_POINT &&
                             x.LineEnd < PDB_HIDDEN_SEQUENCE_POINT).
                            OrderBy((x) => x.Offset).ToArray();

                        if(points.Length == 0) return SourceInfo.FromError(SourceInfoError.NoMatches);

                        PdbSequencePoint p=points[0];
                        int p_index = 0;

                        for (int i = 1; i < points.Length; i++) 
                        {
                            if (points[i].Offset > startOffset) break;

                            p = points[i];
                            p_index = i;
                        }

                        if(p.Offset > startOffset) return SourceInfo.FromError(SourceInfoError.NoMatches);

                        start = p;
                        end = p;
                        ret.CilStart = p.Offset;

                        int pNext_index = p_index + 1;

                        if (pNext_index < points.Length)
                        {
                            pNext = points[pNext_index];
                            ret.CilEnd = pNext.Offset;
                            has_pNext = true;
                        }
                        else
                        {
                            int bodySize = Utils.GetMethodBodySize(m);
                            if (bodySize > 0 && bodySize >= ret.CilStart) ret.CilEnd = (uint)bodySize;
                            has_pNext = false;
                        }
                    }
                    else throw new NotSupportedException("Unknown symbols query type: "+queryType.ToString());

                    ret.SourceFile = coll.File.Name;
                    ret.LineStart = (int)start.LineBegin;
                    ret.LineEnd = (int)end.LineEnd;

                    if (has_pNext && ret.LineStart == ret.LineEnd && start.ColBegin == end.ColEnd)
                    {
                        //C++/CLI
                        end = pNext;
                        ret.LineEnd = (int)end.LineEnd;
                    }

                    bool exact = startOffset != 0 || endOffset != uint.MaxValue;

                    //считаем код метода из исходников
                    ReadSourceFromFile(coll.File.Name, start.LineBegin, start.ColBegin, 
                        end.LineEnd, end.ColEnd, exact, wr);
                    
                }//end foreach
            }//end using

            ret.SourceCode = sb.ToString();
            return ret;
        }

        static SourceInfo GetSourceFromPortablePdb(MethodBase m,
            uint startOffset,
            uint endOffset,
            SymbolsQueryType queryType)
        {
            //построим путь к файлу символов
            string module_path = m.DeclaringType.Assembly.Location;

            if (string.IsNullOrEmpty(module_path))
            {
                return SourceInfo.FromError(SourceInfoError.NoModulePath);
            }

            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            if (!File.Exists(pdb_path))
            {
                throw new SymbolsException("Symbols file not found: " + pdb_path);
            }
            
            SourceLineData[] linedata = PortablePdb.GetSourceLineData(pdb_path, m.MetadataToken);

            if (linedata == null) //not portable PDB file
            {
                return SourceInfo.FromError(SourceInfoError.InvalidFormat); 
            }

            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);
            SourceInfo ret = new SourceInfo();
            ret.SymbolsFile = pdb_path;
            ret.Method = m;
            ret.CilStart = startOffset;
            ret.CilEnd = endOffset;

            string filePath = string.Empty;
            bool foundBegin = false;
            bool foundEnd = false;
            int lineBegin = 0, colBegin = 0;
            int lineEnd = 0, colEnd = 0;            

            for(int i=0;i<linedata.Length;i++)
            {
                SourceLineData line = linedata[i];

                if (string.IsNullOrEmpty(line.FilePath)) continue;

                filePath = line.FilePath;

                if (queryType == SymbolsQueryType.RangeExact)
                {
                    if (line.CilOffset >= startOffset && !foundBegin)
                    {
                        lineBegin = line.LineStart;
                        colBegin = line.ColStart;
                        foundBegin = true;
                    }

                    if (line.CilOffset > endOffset)
                    {
                        SourceLineData endLineData;

                        if (i >= 1) endLineData = linedata[i - 1];
                        else endLineData = line;

                        lineEnd = endLineData.LineEnd;
                        colEnd = endLineData.ColEnd;
                        ret.CilEnd = (uint)line.CilOffset;
                        foundEnd = true;
                        break;
                    }

                    if (i >= linedata.Length - 1)
                    {
                        lineEnd = line.LineEnd;
                        colEnd = line.ColEnd;
                        ret.CilEnd = (uint)line.CilOffset;
                        foundEnd = true;
                        break;
                    }
                }
                else if (queryType == SymbolsQueryType.SequencePoint)
                {
                    if (line.CilOffset > startOffset)
                    {
                        SourceLineData beginLineData;
                        SourceLineData endLineData;

                        if (i >= 1)
                        {
                            beginLineData = linedata[i - 1];
                            endLineData = line;
                            ret.CilEnd = (uint)endLineData.CilOffset;
                        }
                        else
                        {
                            beginLineData = line;

                            if (i < linedata.Length - 1)
                            {
                                endLineData = linedata[i + 1];
                                ret.CilEnd = (uint)endLineData.CilOffset;
                            }
                            else
                            {
                                int bodySize = Utils.GetMethodBodySize(m);
                                if (bodySize > 0 && bodySize >= ret.CilStart) ret.CilEnd = (uint)bodySize;
                            }
                        }

                        lineBegin = beginLineData.LineStart;
                        colBegin = beginLineData.ColStart;
                        lineEnd = beginLineData.LineEnd;
                        colEnd = beginLineData.ColEnd;
                        ret.CilStart = (uint)beginLineData.CilOffset;
                        foundEnd = true;

                        break;
                    }

                    if (i >= linedata.Length - 1)
                    {
                        lineBegin = line.LineStart;
                        colBegin = line.ColStart;
                        lineEnd = line.LineEnd;
                        colEnd = line.ColEnd;
                        ret.CilStart = (uint)line.CilOffset;
                        foundEnd = true;

                        int bodySize = Utils.GetMethodBodySize(m);
                        if (bodySize > 0 && bodySize >= ret.CilStart) ret.CilEnd = (uint)bodySize;
                        
                        break;
                    }
                }
                else throw new NotSupportedException("Unknown symbols query type: " + queryType.ToString());
                
            }//end foreach

            if (string.IsNullOrEmpty(filePath))
            {
                return SourceInfo.FromError(SourceInfoError.NoSourcePath);
            }

            if (!File.Exists(filePath))
            {
                throw new SymbolsException("Source file not found: " + filePath);
            }

            if (!foundEnd)
            {
                return SourceInfo.FromError(SourceInfoError.NoMatches);
            }

            ret.LineStart = lineBegin;
            ret.LineEnd = lineEnd;
            ret.SourceFile = filePath;

            bool exact = startOffset != 0 || endOffset != uint.MaxValue;

            //считаем код метода из исходников
            ReadSourceFromFile(filePath, (uint)lineBegin, (ushort)colBegin, (uint)lineEnd, (ushort)colEnd, exact, wr);

            ret.SourceCode = sb.ToString();
            return ret;
        }

        static void ReadSourceFromFile(string filePath, uint lineBegin, ushort colBegin, 
            uint lineEnd, ushort colEnd, bool exact, TextWriter target)
        {
            //считываем файл исходников
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                        
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
                    if (i >= lineBegin)
                    {
                        //первая строка
                        reading = true;

                        if (exact)
                        {
                            index_start = colBegin - 1;
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
                    if (i >= lineEnd)
                    {
                        //последняя строка
                        index_end = colEnd - 1;
                        if (index_end > line.Length) index_end = line.Length;
                        if (index_end < index_start) index_end = index_start;

                        target.WriteLine(line.Substring(index_start, index_end - index_start));
                        break;
                    }

                    //считывание текущей строки
                    target.WriteLine(line.Substring(index_start, index_end - index_start));
                }
            }//end for

            target.Flush();
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
