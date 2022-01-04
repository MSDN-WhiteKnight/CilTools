/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.SourceCode;
using CilView.Common;
using Internal.Pdb.Portable;
using Internal.Pdb.Windows;

namespace CilView.SourceCode
{
    class PdbUtils
    {
        static readonly Guid GuidSHA256 = Guid.Parse("8829d00f-11b8-4213-878b-770e8597ac16");
        static readonly Guid GuidSHA1 = Guid.Parse("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        static readonly Guid GuidMD5 = Guid.Parse("406ea660-64cf-4c82-b6f0-42d48172a799");

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
            return GetSourceFromPdb(match.Method, 0, uint.MaxValue, SymbolsQueryType.RangeExact).SourceCode;
        }

        static SourceFragment FindFragment(IEnumerable<SourceFragment> fragments, uint offset)
        {
            SourceFragment fragment = null;

            foreach (SourceFragment f in fragments)
            {
                if (fragment == null)
                {
                    fragment = f;
                    continue;
                }

                if (f.CilStart > offset) break;

                fragment = f;
            }

            if (fragment.CilStart > offset) return null;
            else return fragment;
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
            PdbCodeProvider provider = PdbCodeProvider.Instance;
            SourceDocument doc = provider.GetSourceCodeDocuments(m).First();

            if(doc == null) return SourceInfo.FromError(SourceInfoError.NoMatches);

            SourceInfo ret = new SourceInfo();
            ret.Method = m;
            ret.SourceFile = doc.FilePath;
            ret.SymbolsFile = doc.SymbolsFile;

            if (queryType == SymbolsQueryType.RangeExact && startOffset == 0 && endOffset == uint.MaxValue)
            {
                //whole method
                ret.SourceCode = doc.Text;
                ret.CilStart = 0;
                ret.CilEnd = (uint)Utils.GetMethodBodySize(m);
                ret.LineStart = doc.LineStart;
                ret.LineEnd = doc.LineEnd;
                return ret;
            }

            //single sequence point
            SourceFragment fragment = FindFragment(doc.Fragments, startOffset);

            if (fragment == null) return SourceInfo.FromError(SourceInfoError.NoMatches);

            ret.SourceCode = fragment.Text;
            ret.CilStart = (uint)fragment.CilStart;
            ret.CilEnd = (uint)fragment.CilEnd;
            ret.LineStart = fragment.LineStart;
            ret.LineEnd = fragment.LineEnd;
            
            return ret;
        }

        internal static bool IsSourceValid(string file, Guid algo, byte[] hash)
        {
            HashAlgorithm ha=null;

            if (algo.Equals(GuidSHA256))
            {
                ha = SHA256.Create();
            }
            else if (algo.Equals(GuidSHA1))
            {
                ha = SHA1.Create();
            }
            else if (algo.Equals(GuidMD5))
            {
                ha = MD5.Create();
            }
            else if (algo.Equals(Guid.Empty))
            {
                //No hash - skip validation. This usually happens for C++ files.
                return true;
            }

            if (ha == null) throw new SymbolsException("Unsupported PDB hash: "+algo.ToString());

            byte[] data = File.ReadAllBytes(file);
            byte[] hashCalc = ha.ComputeHash(data);
            return Enumerable.SequenceEqual(hashCalc, hash);
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
