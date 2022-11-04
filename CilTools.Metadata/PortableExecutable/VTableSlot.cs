/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Metadata.PortableExecutable
{
    struct VTableSlot
    {
        int tableIndex;
        int slotIndex;

        public VTableSlot(int t, int s)
        {
            this.tableIndex = t;
            this.slotIndex = s;
        }

        public int TableIndex { get { return this.tableIndex; } }

        public int SlotIndex { get { return this.slotIndex; } }
    }
}
