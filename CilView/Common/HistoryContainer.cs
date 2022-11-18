/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Common
{
    public class HistoryContainer<T>
    {
        const int MaxCount = 10;
        List<T> items = new List<T>(MaxCount + 1);
        
        public void Add(T item)
        {
            if (items.Contains(item)) return;

            items.Add(item);

            if (items.Count > MaxCount) items.RemoveAt(0);
        }

        public IEnumerable<T> Items
        {
            get { return items.ToArray(); }
        }
    }
}
