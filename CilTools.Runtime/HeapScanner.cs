/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    internal class HeapScanner
    {
        Dictionary<string, Action<ClrObject>> _actions;

        public HeapScanner() 
        {
            this._actions = new Dictionary<string, Action<ClrObject>>(10, StringComparer.Ordinal);
        }

        /// <summary>
        /// Registers delegate to be executed on objects of specified type
        /// </summary>
        /// <param name="typeName">The full name of type to register action for</param>
        /// <param name="action">Delegate to register</param>
        public void RegisterAction(string typeName, Action<ClrObject> action) 
        {
            this._actions[typeName] = action;
        }

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

        /// <summary>
        /// Executes registered actions for every object in data target's managed heap
        /// </summary>
        /// <param name="dt">Data target to scan heap</param>        
        public void ScanHeap(DataTarget dt)
        {
            foreach (ClrInfo runtimeInfo in dt.ClrVersions)
            {
                ClrRuntime runtime = runtimeInfo.CreateRuntime();
                IEnumerable<ClrObject> en = runtime.Heap.EnumerateObjects();

                foreach (ClrObject o in en)
                {
                    if (o.Type == null) continue;
                    if (o.Type.Name == null) continue;

                    string typeName = o.Type.Name;
                    Action<ClrObject> action;

                    if (this._actions.TryGetValue(typeName, out action)) 
                    {
                        action(o);
                    }
                }
            }
        }
    }
}
