/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Metadata.PortableExecutable
{
    /// <summary>
    /// Contains the VTable slot address (ECMA-335 II.25.3.3.3). The slot is identified by the VTable index 
    /// and the slot index.
    /// </summary>
    struct VTableSlot
    {
        int tableIndex;
        int slotIndex;

        public VTableSlot(int t, int s)
        {
            this.tableIndex = t;
            this.slotIndex = s;
        }

        /// <summary>
        /// Gets zero-based index of VTable within the contatining module. The negative value indicates that 
        /// VTable slot is not found.
        /// </summary>
        public int TableIndex { get { return this.tableIndex; } }

        /// <summary>
        /// Gets zero-based index of the slot within a VTable. The negative value indicates that 
        /// VTable slot is not found.
        /// </summary>
        public int SlotIndex { get { return this.slotIndex; } }
    }
}
