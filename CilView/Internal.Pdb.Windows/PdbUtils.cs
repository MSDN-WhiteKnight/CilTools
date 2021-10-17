/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Internal.Pdb.Windows
{
    class PdbUtils
    {
        public static string GetSourceFromPdb<T>(Predicate<T> match) 
        {
            return GetSourceFromPdb(match.Method,0,uint.MaxValue,true);
        }

        public static string GetSourceFromPdb(MethodBase m,uint startOffset,uint endOffset,bool exact)
        {
            int token = m.MetadataToken;

            //построим путь к файлу символов
            string module_path = m.DeclaringType.Assembly.Location;
            if(string.IsNullOrEmpty(module_path)) return string.Empty;

            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            if (!File.Exists(pdb_path)) 
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            PdbReader reader = new PdbReader(pdb_path);

            using (reader)
            {
                //найдем метод в символах
                var func = reader.GetFunctionFromToken((uint)token);

                if(func==null) return string.Empty;
                if(func.SequencePoints==null) return string.Empty;

                foreach (PdbSequencePointCollection coll in func.SequencePoints)
                {
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
                            return string.Empty;
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
                            return string.Empty;
                        }

                        start = points_sorted.Last();
                        end = start;
                    }

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
                                index_start = start.ColBegin - 1;
                                if (index_start < 0) index_start = 0;
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
                }

            }

            return sb.ToString();
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
