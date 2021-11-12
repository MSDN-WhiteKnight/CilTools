/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Internal.Pdb.Portable
{
    static class PortablePdb
    {
        static string ReadDocumentPath(MetadataReader reader, DocumentHandle dh)
        {
            Document doc = reader.GetDocument(dh);
            BlobReader blob = reader.GetBlobReader(doc.Name);
            char separator = (char)blob.ReadByte();
            StringBuilder sb = new StringBuilder(300);

            while (true)
            {
                BlobHandle bh = blob.ReadBlobHandle();

                if (!bh.IsNil)
                {
                    byte[] nameBytes = reader.GetBlobBytes(bh);
                    sb.Append(Encoding.UTF8.GetString(nameBytes));
                }

                if (blob.Offset >= blob.Length) break;

                sb.Append(separator);
            }

            string path = sb.ToString();
            return path;
        }

        static bool IsPortablePdb(FileStream fs)
        {
            if (fs.Length < 4) return false; //too short to be valid metadata

            bool ret;

            try
            {
                //read first 4 bytes
                byte b1 = (byte)fs.ReadByte();
                byte b2 = (byte)fs.ReadByte();
                byte b3 = (byte)fs.ReadByte();
                byte b4 = (byte)fs.ReadByte();

                //CLI metadata signature (ECMA-335 II.24.2.1 - Metadata root)
                if (b1 == 0x42 && b2 == 0x53 && b3 == 0x4A && b4 == 0x42)
                {
                    ret = true;
                }
                else
                {
                    ret = false;
                }
            }
            finally
            {
                fs.Seek(0, SeekOrigin.Begin);
            }

            return ret;
        }

        /// <summary>
        /// Gets the collection of mappings between CIL offsets and source code lines of the specified method 
        /// from Portable PDB file. Returns null if the file is not in Portable PDB format.
        /// </summary>
        /// <param name="pdbPath">Path to PDB file</param>
        /// <param name="methodToken">Metadata token of method in MethodDef table</param>
        /// <returns>
        /// The collection of source line mappings, or null if the file is not a Portable PDB file
        /// </returns>
        public static SourceLineData[] GetSourceLineData(string pdbPath, int methodToken)
        {
            //determine method row number
            EntityHandle ehMethod = MetadataTokens.EntityHandle(methodToken);

            if (ehMethod.Kind != HandleKind.MethodDefinition &&
                ehMethod.Kind != HandleKind.MethodDebugInformation)
            {
                throw new ArgumentException("Invalid token: " + ehMethod.Kind.ToString(), "methodToken");
            }

            int rowNumber = MetadataTokens.GetRowNumber(ehMethod);

            //MethodDebugInformation row number is the same
            MethodDebugInformationHandle hDebug = MetadataTokens.MethodDebugInformationHandle(rowNumber);

            //open Portable PDB file
            FileStream fs = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //check file format
            if (!IsPortablePdb(fs)) 
            {
                fs.Dispose();
                return null;
            }

            MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(fs);
            List<SourceLineData> ret=new List<SourceLineData>(100);

            using (provider)
            {
                //stream is disposed by MetadataReaderProvider
                MetadataReader reader = provider.GetMetadataReader();

                if (rowNumber > reader.MethodDebugInformation.Count)
                {
                    throw new ArgumentOutOfRangeException("methodToken");
                }

                MethodDebugInformation di = reader.GetMethodDebugInformation(hDebug);
                string path = string.Empty;

                //file path if all sequence points are in the same file
                if (!di.Document.IsNil) path = ReadDocumentPath(reader, di.Document);

                foreach (SequencePoint sp in di.GetSequencePoints())
                {
                    if (sp.IsHidden) continue;

                    string s = path;

                    if (!sp.Document.IsNil && sp.Document != di.Document)
                    {
                        //file path if method spans over nultiple files
                        s = ReadDocumentPath(reader, di.Document);
                    }

                    ret.Add(new SourceLineData()
                    {
                        FilePath = s,
                        CilOffset = sp.Offset,
                        LineStart = sp.StartLine,
                        LineEnd = sp.EndLine,
                        ColStart = sp.StartColumn,
                        ColEnd = sp.EndColumn
                    });
                }
            }//end using

            return ret.ToArray();
        }
    }
}
