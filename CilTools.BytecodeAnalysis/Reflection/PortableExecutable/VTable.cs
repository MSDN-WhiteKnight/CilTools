/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Reflection.PortableExecutable
{
    /// <summary>
    /// Represents a table of pointers to virtual functions in executable image
    /// </summary>
    public class VTable
    {
        //ECMA-335 II.25.3.3.3 - Vtable fixup
        int _rva;
        short _n_items;
        short _type;
        byte[] _data;

        internal const short COR_VTABLE_64BIT = 0x02;
        internal const short COR_VTABLE_FROM_UNMANAGED = 0x04; //Transition from unmanaged to managed code
        internal const short COR_VTABLE_CALL_MOST_DERIVED = 0x10; //Call most derived method described by the token

        // retainappdomain - not included in ECMA-335, but used in Microsoft implementation
        internal const short COR_VTABLE_RETAINAPPDOMAIN = 0x08; 

        /// <summary>
        /// Creates a new VTable
        /// </summary>
        public VTable(int rva, short n_items, short type, byte[] data)
        {
            this._rva = rva;
            this._n_items = n_items;
            this._type = type;
            this._data = data;
        }

        /// <summary>
        /// Gets a Relative virtual address of this table's data in the image it was read from
        /// </summary>
        public int RVA { get { return this._rva; } }

        /// <summary>
        /// Gets the number of slots (functions) in this table
        /// </summary>
        public short SlotsCount { get { return this._n_items; } }

        /// <summary>
        /// Gets the short integer value that represents the type of this table
        /// </summary>
        public short Type { get { return this._type; } }

        /// <summary>
        /// Gets the boolean value indicating whether this table contains 64-bit function pointer values
        /// </summary>
        public bool Is64Bit
        {
            get { return (this._type & COR_VTABLE_64BIT) != 0; }
        }

        /// <summary>
        /// Gets the data of this table as a byte array
        /// </summary>
        public byte[] GetData()
        {
            byte[] ret = new byte[this._data.Length];
            Array.Copy(this._data, ret, ret.Length);
            return ret;
        }

        /// <summary>
        /// Gets the VTable slot initial value as <c>Int64</c>. The initial slot value is a method's metadata token, 
        /// which gets replaced with a native function address when the image is loaded.
        /// </summary>
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

        /// <summary>
        /// Gets the VTable slot initial value as <c>Int32</c>. If the value is 64-bit, it is converted to <c>Int32</c>. 
        /// The initial slot value is a method's metadata token, which gets replaced with a native function address when 
        /// the image is loaded.
        /// </summary>
        public int GetSlotValueInt32(int index)
        {
            return (int)this.GetSlotValue(index);
        }
    }
}
