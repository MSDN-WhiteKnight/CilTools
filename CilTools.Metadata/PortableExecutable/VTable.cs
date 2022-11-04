/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace CilTools.Metadata.PortableExecutable
{
    class VTable
    {
        //ECMA-335 II.25.3.3.3 - Vtable fixup
        int _rva;
        short _n_items;
        short _type;
        byte[] _data;

        internal const short COR_VTABLE_64BIT = 0x02;

        public VTable(int rva, short n_items, short type, ImmutableArray<byte> data)
        {
            this._rva = rva;
            this._n_items = n_items;
            this._type = type;
            List<byte> bytes = new List<byte>(data);
            this._data = bytes.ToArray();
        }

        public int RVA { get { return this._rva; } }

        public short SlotsCount { get { return this._n_items; } }

        public short Type { get { return this._type; } }

        public bool Is64Bit
        {
            get { return (this._type & COR_VTABLE_64BIT) != 0; }
        }

        public byte[] GetData()
        {
            byte[] ret = new byte[this._data.Length];
            Array.Copy(this._data, ret, ret.Length);
            return ret;
        }

        public long GetSlotValue(int index)
        {
            int slot_size;

            if (this.Is64Bit)
            {
                slot_size = 8;
            }
            else
            {
                slot_size = 4;
            }

            int pos = index * slot_size;
            byte[] slot_data = new byte[slot_size];
            Array.Copy(this._data, pos, slot_data, 0, slot_size);

            if (this.Is64Bit)
            {
                return BitConverter.ToInt64(slot_data, 0);
            }
            else
            {
                return BitConverter.ToInt32(slot_data, 0);
            }
        }

        public int GetSlotValueInt32(int index)
        {
            return (int)this.GetSlotValue(index);
        }
    }
}
