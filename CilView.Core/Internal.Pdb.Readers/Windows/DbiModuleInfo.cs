// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file for more information.

using System.IO;

namespace Internal.Pdb.Windows
{
    internal class DbiModuleInfo
    {
        internal DbiModuleInfo(BitAccess bits, bool readStrings)
        {
            bits.ReadInt32(out opened);
            new DbiSecCon(bits);
            bits.ReadUInt16(out flags);
            bits.ReadInt16(out stream);
            bits.ReadInt32(out cbSyms);
            bits.ReadInt32(out cbOldLines);
            bits.ReadInt32(out cbLines);
            bits.ReadInt16(out files);
            bits.ReadInt16(out pad1);
            bits.ReadUInt32(out offsets);
            bits.ReadInt32(out niSource);
            bits.ReadInt32(out niCompiler);
            if (readStrings)
            {
                bits.ReadCString(out moduleName);
                bits.ReadCString(out objectName);
            }
            else
            {
                bits.SkipCString(out moduleName);
                bits.SkipCString(out objectName);
            }

            bits.Align(4);
        }

        public override string ToString()
        {
            return Path.GetFileName(moduleName);
        }

        internal int opened; //  0..3
        //internal DbiSecCon section;                //  4..31
        internal ushort flags; // 32..33
        internal short stream; // 34..35
        internal int cbSyms; // 36..39
        internal int cbOldLines; // 40..43
        internal int cbLines; // 44..57
        internal short files; // 48..49
        internal short pad1; // 50..51
        internal uint offsets;
        internal int niSource;
        internal int niCompiler;
        internal string moduleName;
        internal string objectName;
    }
}