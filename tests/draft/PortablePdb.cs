using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ConsoleApp1
{
    public class SourceLineData
    {
        public string FilePath { get; set; }
        public int CilOffset { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public int ColStart { get; set; }
        public int ColEnd { get; set; }
    }

    class Program
    {
        public static string ReadDocumentPath(MetadataReader reader, DocumentHandle dh)
        {
            Document doc = reader.GetDocument(dh);
            BlobReader blob = reader.GetBlobReader(doc.Name);
            char separator = (char)blob.ReadByte();
            StringBuilder sb = new StringBuilder(300);

            while (true)
            {
                BlobHandle bh = blob.ReadBlobHandle();
                if (bh.IsNil) break;

                byte[] nameBytes = reader.GetBlobBytes(bh);
                sb.Append(Encoding.UTF8.GetString(nameBytes));

                if (blob.Offset >= blob.Length) break;

                sb.Append(separator);
            }

            string path = sb.ToString();
            return path;
        }

        public static IEnumerable<SourceLineData> GetSourceLineData(string pdbPath, int methodToken)
        {
            //determine method row number
            EntityHandle ehMethod = MetadataTokens.EntityHandle(methodToken);

            if (ehMethod.Kind != HandleKind.MethodDefinition &&
                ehMethod.Kind != HandleKind.MethodDebugInformation)
            {
                throw new ArgumentException("Invalid token: " + ehMethod.Kind.ToString(),"methodToken");
            }
                        
            int rowNumber = MetadataTokens.GetRowNumber(ehMethod);

            //MethodDebugInformation row number is the same
            MethodDebugInformationHandle hDebug = MetadataTokens.MethodDebugInformationHandle(rowNumber);

            //open Portable PDB file
            FileStream fs = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(fs);

            using (provider)
            {
                //stream is disposed by MetadataReaderProvider
                MetadataReader reader = provider.GetMetadataReader();

                if (rowNumber > reader.MethodDebugInformation.Count)
                {
                    throw new ArgumentOutOfRangeException("methodToken");
                }

                MethodDebugInformation di=reader.GetMethodDebugInformation(hDebug);
                string path = string.Empty;

                //file path if all sequence points are in the same file
                if (!di.Document.IsNil) path = ReadDocumentPath(reader, di.Document);

                foreach (SequencePoint sp in di.GetSequencePoints())
                {
                    if (sp.IsHidden) continue;

                    string s = path;

                    if (!sp.Document.IsNil && sp.Document!=di.Document)
                    {
                        //file path if method spans over nultiple files
                        s=ReadDocumentPath(reader, di.Document);
                    }

                    yield return new SourceLineData()
                    {
                        FilePath = s,
                        CilOffset = sp.Offset,
                        LineStart = sp.StartLine,
                        LineEnd = sp.EndLine,
                        ColStart = sp.StartColumn,
                        ColEnd = sp.EndColumn
                    };
                }
            }//end using
        }

        public static void Main(string[] args)
        {
            MethodBase method = typeof(Program).GetMethod("Main");
            Console.WriteLine("0x"+method.MetadataToken.ToString("X"));
            EntityHandle ehMethod=MetadataTokens.EntityHandle(method.MetadataToken);
            Console.WriteLine(MetadataTokens.GetRowNumber(ehMethod).ToString());

            //построим путь к файлу символов
            string module_path = method.Module.FullyQualifiedName;
            string pdb_path = Path.Combine(
                Path.GetDirectoryName(module_path),
                Path.GetFileNameWithoutExtension(module_path) + ".pdb"
                );

            //0x6000010
            IEnumerable<SourceLineData> source = GetSourceLineData(pdb_path, method.MetadataToken);

            foreach (SourceLineData item in source)
            {
                Console.WriteLine("Offset: " + item.CilOffset.ToString());
                Console.WriteLine("StartLine: " + item.LineStart.ToString());
                Console.WriteLine("EndLine: " + item.LineEnd.ToString());
                Console.WriteLine("Document: " + item.FilePath);
                Console.WriteLine();
            }
                
            Console.WriteLine("End");
            Console.ReadKey();
        }
    }
}
