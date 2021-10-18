/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Internal.Pdb.Windows;

namespace CilView.Symbols
{
    class PdbUtils
    {
        public static string GetSourceFromPdb<T>(Predicate<T> match) 
        {
            return GetSourceFromPdb(match.Method,0,uint.MaxValue,true).SourceCode;
        }

        /// <summary>
        /// Gets the source code for the specified bytecode fragment using PDB symbols
        /// </summary>
        /// <param name="m">Method for which to get the source code</param>
        /// <param name="startOffset">Byte offset of the bytecode fragment's start</param>
        /// <param name="endOffset">Byte offset of the bytecode fragment's end</param>
        /// <param name="exact">
        /// <c>true</c> to return the sources for exact specified bytecode fragment; <c>false</c> to get the sources 
        /// for the sequence point in which starting offset lies.
        /// </param>
        public static SourceInfo GetSourceFromPdb(MethodBase m,uint startOffset,uint endOffset,bool exact)
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

                    if (exact)
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
                    else 
                    {
                        var points_sorted = coll.Lines.
                            Where((x) => x.LineBegin <= lines.Length && x.LineEnd <= lines.Length &&
                            x.Offset <= startOffset).
                            OrderBy((x) => x.Offset);

                        if (points_sorted.Count() == 0)
                        {
                            return SourceInfo.Empty;
                        }

                        start = points_sorted.Last();
                        end = start;
                    }

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

        public static void Test()
        {
            string source;
            Console.WriteLine("*** Source from PDB: ***");
            source = GetSourceFromPdb<string>((s) => s == "Test" || s.Length == 0);
            Console.WriteLine(source);
        }
    }
}
