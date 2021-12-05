/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.SourceCode;
using CilView.Common;
using Internal.Pdb.Portable;
using Internal.Pdb.Windows;

namespace CilView.SourceCode
{
    class PdbCodeProvider : ICodeProvider
    {
        public IEnumerable<SourceDocument> GetSourceCodeDocuments(MethodBase m)
        {
            return new SourceDocument[] { GetSourceFromWindowsPdb(m) };
        }

        static SourceDocument GetSourceFromWindowsPdb(MethodBase m)
        {
            int token = m.MetadataToken;

            //построим путь к файлу символов
            string module_path = m.DeclaringType.Assembly.Location;

            if (string.IsNullOrEmpty(module_path))
            {
                throw new SymbolsException("Cannot find symbols: assembly does not have a file path");
            }

            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            if (!File.Exists(pdb_path))
            {
                throw new SymbolsException("Symbols file not found: " + pdb_path);
            }

            PdbReader reader = new PdbReader(pdb_path);
            SourceDocument ret = new SourceDocument();
            ret.SymbolsFile = pdb_path;
            ret.Method = m;

            using (reader)
            {
                //найдем метод в символах
                var func = reader.GetFunctionFromToken((uint)token);

                if (func == null) return null;

                if (func.SequencePoints == null)
                {
                    throw new SymbolsException("Symbols does not contain sequence points for the specified method");
                }

                foreach (PdbSequencePointCollection coll in func.SequencePoints)
                {
                    if (coll.File == null) continue;
                    if (string.IsNullOrEmpty(coll.File.Name)) continue;

                    if (!File.Exists(coll.File.Name))
                    {
                        throw new SymbolsException("Source file not found: " + coll.File.Name);
                    }

                    bool isValid = PdbUtils.IsSourceValid(coll.File.Name, coll.File.AlgorithmId, coll.File.Checksum);

                    if (!isValid)
                    {
                        throw new SymbolsException("Source file does not match PDB hash: " + coll.File.Name);
                    }

                    PdbSequencePoint[] points_sorted = coll.Lines.
                        Where((x) => x.LineBegin < PdbUtils.PDB_HIDDEN_SEQUENCE_POINT &&
                        x.LineEnd < PdbUtils.PDB_HIDDEN_SEQUENCE_POINT).
                        OrderBy((x) => x.Offset).ToArray();

                    if (points_sorted.Length == 0)
                    {
                        throw new SymbolsException("Symbols does not contain sequence points for the specified method");
                    }

                    PdbSequencePoint start = points_sorted[0];
                    PdbSequencePoint end = points_sorted.Last();
                    ret.FilePath = coll.File.Name;

                    for (int i = 0; i < points_sorted.Length; i++)
                    {
                        PdbSequencePoint sp = points_sorted[i];
                        SourceFragment fragment = new SourceFragment();

                        //найдем номера строк в файле, соответствующие началу и концу фрагмента
                        int lineStart = (int)sp.LineBegin;
                        int lineEnd = (int)sp.LineEnd;
                        int colStart = sp.ColBegin;
                        int colEnd = sp.ColEnd;
                        fragment.CilStart = (int)sp.Offset;

                        if (i == points_sorted.Length - 1)
                        {
                            //last
                            int bodySize = Utils.GetMethodBodySize(m);
                            if (bodySize > 0 && bodySize >= fragment.CilStart) fragment.CilEnd = bodySize;
                        }
                        else
                        {
                            fragment.CilEnd = (int)points_sorted[i + 1].Offset;

                            if (lineStart == lineEnd && colStart == colEnd)
                            {
                                //C++/CLI
                                lineEnd = (int)points_sorted[i + 1].LineBegin;
                                colEnd = (int)points_sorted[i + 1].ColEnd;
                            }
                        }

                        fragment.LineStart = lineStart;
                        fragment.LineEnd = lineEnd;

                        //считаем код метода из исходников
                        string s = PdbUtils.ReadSourceFromFile(coll.File.Name, (uint)lineStart, (ushort)colStart,
                            (uint)lineEnd, (ushort)colEnd, exact: true);

                        fragment.Text = s;
                        ret.AddFragment(fragment);
                    }//end for

                    break;
                }//end foreach
            }//end using

            return ret;
        }

        public static string PrintDocument(SourceDocument doc)
        {
            if (doc == null) return "(No source code)";

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);
            wr.WriteLine(doc.FilePath);
            wr.WriteLine("Method: " + doc.Method.Name);
            wr.WriteLine("Symbols: " + doc.SymbolsFile);
            wr.WriteLine();
            int n = 1;

            foreach (SourceFragment fragment in doc.Fragments)
            {
                wr.WriteLine(" Fragment #"+n.ToString());
                wr.Write("[CIL range: " + fragment.CilStart.ToString() + " - ");
                wr.WriteLine(fragment.CilEnd.ToString() + "]");
                wr.Write("[Lines: " + fragment.LineStart.ToString() + " - ");
                wr.WriteLine(fragment.LineEnd.ToString() + "]");
                wr.WriteLine(fragment.Text);
                wr.WriteLine();
                n++;
            }

            return sb.ToString();
        }
    }
}
