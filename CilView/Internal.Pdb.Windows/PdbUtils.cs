/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Internal.Pdb.Windows
{
    class PdbUtils
    {
        public static string GetSourceFromPdb<T>(Predicate<T> match)
        {
            int token = match.Method.MetadataToken;

            //построим путь к файлу символов
            string module_path = match.Method.Module.FullyQualifiedName;
            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            StringBuilder sb = new StringBuilder();
            PdbReader reader = new PdbReader(pdb_path);

            using (reader)
            {
                //найдем метод в символах
                var func = reader.GetFunctionFromToken((uint)token);

                foreach (PdbSequencePointCollection coll in func.SequencePoints)
                {
                    //считываем файл исходников
                    string[] lines = File.ReadAllLines(coll.File.Name, System.Text.Encoding.UTF8);

                    //найдем номера строк в файле, соответствующие началу и концу метода
                    var points_sorted = coll.Lines.
                        Where<PdbSequencePoint>((x) => x.LineBegin <= lines.Length && x.LineEnd <= lines.Length).
                        OrderBy<PdbSequencePoint, uint>((x) => x.Offset);
                    PdbSequencePoint start = points_sorted.First();
                    PdbSequencePoint end = points_sorted.Last();

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
