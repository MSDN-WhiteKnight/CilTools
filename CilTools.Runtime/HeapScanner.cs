/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    internal static class HeapScanner
    {
        /// <summary>
        /// Executes the specified delegate for every object in data target's managed heap
        /// </summary>
        /// <param name="dt">Data target to scan heap</param>
        /// <param name="action">Delegate that receives objects from heap</param>
        public static void ScanHeap(DataTarget dt,Action<ClrObject> action)
        {
            foreach (ClrInfo runtimeInfo in dt.ClrVersions)
            {
                ClrRuntime runtime = runtimeInfo.CreateRuntime();
                IEnumerable<ClrObject> en = runtime.Heap.EnumerateObjects();

                foreach (ClrObject o in en)
                {
                    if (o.Type == null) continue;
                    if (o.Type.Name == null) continue;
                    
                    action(o);
                }
            }
        }
    }
}
