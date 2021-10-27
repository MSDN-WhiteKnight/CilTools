// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file for more information.

using System;

namespace Internal.Pdb.Windows
{
    /// <summary>
    /// Represents a scope within a function or class.
    /// </summary>
    public class PdbScope
    {
        /// <summary>
        /// A list of constants defined in this scope.
        /// </summary>
        public PdbConstant[] Constants { get; }

        /// <summary>
        /// A list of variable slots in this function.
        /// </summary>
        public PdbSlot[] Slots { get; }

        /// <summary>
        /// A list of sub-scopes within this scope.
        /// </summary>
        public PdbScope[] Scopes { get; }

        /// <summary>
        /// A list of namespaces used in this scope.
        /// </summary>
        public string[] UsedNamespaces { get; }

        /// <summary>
        /// The address of this scope.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// The IL offset of this scope.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// The length of this scope.
        /// </summary>
        public uint Length { get; }

        internal PdbScope(uint address, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces)
        {
            Constants = constants;
            Slots = slots;
            Scopes = new PdbScope[0];
            UsedNamespaces = usedNamespaces;
            Address = address;
            Offset = 0;
            Length = length;
        }

        internal PdbScope(uint funcOffset, BlockSym32 block, BitAccess bits, out uint typind)
        {
            //this.segment = block.seg;
            Address = block.off;
            Offset = block.off - funcOffset;
            Length = block.len;
            typind = 0;

            int constantCount;
            int scopeCount;
            int slotCount;
            int namespaceCount;
            PdbFunction.CountScopesAndSlots(bits, block.end, out constantCount, out scopeCount, out slotCount, out namespaceCount);
            Constants = new PdbConstant[constantCount];
            Scopes = new PdbScope[scopeCount];
            Slots = new PdbSlot[slotCount];
            UsedNamespaces = new string[namespaceCount];
            int constant = 0;
            int scope = 0;
            int slot = 0;
            int usedNs = 0;

            while (bits.Position < block.end)
            {
                ushort siz;
                ushort rec;

                bits.ReadUInt16(out siz);
                int star = bits.Position;
                int stop = bits.Position + siz;
                bits.Position = star;
                bits.ReadUInt16(out rec);

                switch ((SYM)rec)
                {
                    case SYM.S_BLOCK32:
                    {
                        BlockSym32 sub = new BlockSym32();

                        bits.ReadUInt32(out sub.parent);
                        bits.ReadUInt32(out sub.end);
                        bits.ReadUInt32(out sub.len);
                        bits.ReadUInt32(out sub.off);
                        bits.ReadUInt16(out sub.seg);
                        bits.SkipCString(out sub.name);

                        bits.Position = stop;
                        Scopes[scope++] = new PdbScope(funcOffset, sub, bits, out typind);
                        break;
                    }

                    case SYM.S_MANSLOT:
                        Slots[slot++] = new PdbSlot(bits, out typind);
                        bits.Position = stop;
                        break;

                    case SYM.S_UNAMESPACE:
                        bits.ReadCString(out UsedNamespaces[usedNs++]);
                        bits.Position = stop;
                        break;

                    case SYM.S_END:
                        bits.Position = stop;
                        break;

                    case SYM.S_MANCONSTANT:
                        Constants[constant++] = new PdbConstant(bits);
                        bits.Position = stop;
                        break;

                    default:
                        //throw new PdbException("Unknown SYM in scope {0}", (SYM)rec);
                        bits.Position = stop;
                        break;
                }
            }

            if (bits.Position != block.end)
            {
                throw new Exception("Not at S_END");
            }

            ushort esiz;
            ushort erec;
            bits.ReadUInt16(out esiz);
            bits.ReadUInt16(out erec);

            if (erec != (ushort)SYM.S_END)
            {
                throw new Exception("Missing S_END");
            }
        }
    }
}