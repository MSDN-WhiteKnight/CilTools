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
    class PdbCodeProvider : SourceCodeProvider
    {
        public override IEnumerable<SourceDocument> GetSourceCodeDocuments(MethodBase m)
        {
            SourceDocument ret;

            try
            {
                //try Portable PDB first
                ret = GetSourceFromPortablePdb(m);
            }
            catch (BadImageFormatException)
            {
                ret = null;
            }

            if (ret == null)
            {
                //read Windows PDB if failed
                ret = GetSourceFromWindowsPdb(m);
            }
            
            return new SourceDocument[] { ret };
        }
        
        static string GetSymbolsPath(MethodBase m) 
        {
            //построим путь к файлу символов
            string module_path = m.DeclaringType.Assembly.Location;

            if (string.IsNullOrEmpty(module_path))
            {
                throw new SymbolsException("Cannot find symbols: assembly does not have a file path");
            }

            string pdb_path = Path.Combine(Path.GetDirectoryName(module_path),
                                Path.GetFileNameWithoutExtension(module_path) + ".pdb");

            if (!File.Exists(pdb_path))
            {
                throw new SymbolsException("Symbols file not found: " + pdb_path);
            }

            return pdb_path;
        }

        static SourceDocument GetSourceFromPortablePdb(MethodBase m)
        {
            //построим путь к файлу символов
            string pdb_path = GetSymbolsPath(m);
            
            SourceLineData[] linedata = PortablePdb.GetSourceLineData(pdb_path, m.MetadataToken);

            if (linedata == null) //not portable PDB file
            {
                return null;
            }

            SourceDocument ret = new SourceDocument();
            ret.SymbolsFile = pdb_path;
            ret.Method = m;

            string filePath = string.Empty;
            
            for (int i = 0; i < linedata.Length; i++)
            {
                SourceLineData line = linedata[i];

                if (string.IsNullOrEmpty(line.FilePath)) continue;

                filePath = line.FilePath;

                if (!File.Exists(filePath))
                {
                    throw new SymbolsException("Source file not found: " + filePath);
                }

                bool isValid = PdbUtils.IsSourceValid(filePath, line.HashAlgorithm, line.Hash);

                if (!isValid)
                {
                    throw new SymbolsException("Source file does not match PDB hash: " + filePath);
                }

                SourceFragment fragment = new SourceFragment();
                fragment.CilStart = line.CilOffset;
                fragment.LineStart = line.LineStart;
                fragment.LineEnd = line.LineEnd;
                int colStart = line.ColStart;
                int colEnd = line.ColEnd;
                fragment.ColStart = colStart;
                fragment.ColEnd = colEnd;

                if (i == linedata.Length - 1)
                {
                    //last
                    int bodySize = Utils.GetMethodBodySize(m);
                    if (bodySize > 0 && bodySize >= fragment.CilStart) fragment.CilEnd = bodySize;
                }
                else 
                {
                    fragment.CilEnd = linedata[i + 1].CilOffset;
                }

                //считаем код фрагмента из исходников
                string s = PdbUtils.ReadSourceFromFile(filePath, (uint)fragment.LineStart, (ushort)colStart, 
                    (uint)fragment.LineEnd, (ushort)colEnd, exact: true);

                fragment.Text = s;
                ret.AddFragment(fragment);
            }//end for

            if (string.IsNullOrEmpty(filePath))
            {
                throw new SymbolsException("Source path not found in symbols");
            }

            if (linedata.Length > 0) 
            {
                SourceLineData start = linedata[0];
                SourceLineData end = linedata.Last();
                ret.LineStart = start.LineStart;
                ret.ColStart = start.ColStart;
                ret.LineEnd = end.LineEnd;
                ret.ColEnd = end.ColEnd;

                //считаем код метода из исходников
                string methodSource = PdbUtils.ReadSourceFromFile(filePath, (uint)ret.LineStart, (ushort)ret.ColStart,
                    (uint)ret.LineEnd, (ushort)ret.ColEnd, exact: false);

                ret.Text = methodSource;
            }

            ret.FilePath = filePath;
            return ret;
        }

        static SourceDocument GetSourceFromWindowsPdb(MethodBase m)
        {
            int token = m.MetadataToken;

            //построим путь к файлу символов
            string pdb_path = GetSymbolsPath(m);                        

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

                    int lineStart;
                    int lineEnd;
                    int colStart;
                    int colEnd;
                    PdbSequencePoint start = points_sorted[0];
                    PdbSequencePoint end = points_sorted.Last();
                    ret.FilePath = coll.File.Name;

                    for (int i = 0; i < points_sorted.Length; i++)
                    {
                        PdbSequencePoint sp = points_sorted[i];
                        SourceFragment fragment = new SourceFragment();

                        //найдем номера строк в файле, соответствующие началу и концу фрагмента
                        lineStart = (int)sp.LineBegin;
                        lineEnd = (int)sp.LineEnd;
                        colStart = sp.ColBegin;
                        colEnd = sp.ColEnd;
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
                        fragment.ColStart = colStart;
                        fragment.ColEnd = colEnd;

                        //считаем код фрагмента из исходников
                        string s = PdbUtils.ReadSourceFromFile(coll.File.Name, (uint)lineStart, (ushort)colStart,
                            (uint)lineEnd, (ushort)colEnd, exact: true);

                        fragment.Text = s;
                        ret.AddFragment(fragment);
                    }//end for

                    //найдем номера строк в файле, соответствующие началу и концу метода
                    lineStart = (int)start.LineBegin;
                    lineEnd = (int)end.LineEnd;
                    colStart = start.ColBegin;
                    colEnd = end.ColEnd;

                    ret.LineStart = lineStart;
                    ret.LineEnd = lineEnd;
                    ret.ColStart = colStart;
                    ret.ColEnd = colEnd;

                    //считаем код метода из исходников
                    string methodSource = PdbUtils.ReadSourceFromFile(coll.File.Name, (uint)lineStart, (ushort)colStart,
                        (uint)lineEnd, (ushort)colEnd, exact: false);
                    ret.Text = methodSource;

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
