/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CilBytecodeParser;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    public class ClrAssemblyReader
    {
        static IEnumerable<MethodBase> EnumerateDynamicMethods(DataTarget dt)
        {
            foreach (ClrInfo runtimeInfo in dt.ClrVersions)
            {
                ClrRuntime runtime = runtimeInfo.CreateRuntime();

                //dump dynamic methods
                var en = runtime.Heap.EnumerateObjects();

                foreach (ClrObject o in en)
                {
                    if (o.Type == null) continue;

                    var bt = o.Type.BaseType;

                    if (o.Type.Name == "System.Reflection.Emit.DynamicMethod" || o.Type.Name == "System.Reflection.Emit.MethodBuilder")
                    {
                        ClrDynamicMethod dm = new ClrDynamicMethod(o);
                        yield return dm;
                    }
                }
            }
        }

        public static IEnumerable<MethodBase> EnumerateDynamicMethods(Process process)
        {
            if (process == null) throw new ArgumentNullException("process");

            DataTarget dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

            using (dt)
            {
                foreach (MethodBase m in EnumerateDynamicMethods(dt)) yield return m;
            }
        }

        public static IEnumerable<MethodBase> EnumerateMethods(Process process)
        {
            if (process == null) throw new ArgumentNullException("process");

            //Start ClrMD session
            DataTarget dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

            using (dt)
            {
                foreach (ClrInfo runtimeInfo in dt.ClrVersions)
                {
                    ClrRuntime runtime = runtimeInfo.CreateRuntime();
                    ClrAssemblyReader reader = new ClrAssemblyReader(runtime);

                    //dump regular methods
                    foreach (ClrModule module in runtime.Modules)
                    {
                        ClrAssemblyInfo ass = reader.Read(module);

                        foreach (MethodBase m in ass.EnumerateMethods())
                        {
                            yield return m;
                        }
                    }                    
                }

                //dump dynamic methods                     
                var en = EnumerateDynamicMethods(dt);

                foreach (var o in en)
                {
                    yield return o;
                }

            }//end using
        }

        public static IEnumerable<MethodBase> EnumerateModuleMethods(Process process, string modulename = "")
        {
            if (process == null) throw new ArgumentNullException("process");

            if (String.IsNullOrEmpty(modulename))
            {
                modulename = Path.GetFileName(process.MainModule.FileName);
            }

            //Start ClrMD session
            DataTarget dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

            using (dt)
            {
                foreach (ClrInfo runtimeInfo in dt.ClrVersions)
                {
                    ClrRuntime runtime = runtimeInfo.CreateRuntime();
                    ClrAssemblyReader reader = new ClrAssemblyReader(runtime);

                    //dump regular methods
                    foreach (ClrModule module in runtime.Modules)
                    {
                        if (module.FileName == null) continue;

                        if (Path.GetFileName(module.FileName).Equals(modulename, StringComparison.InvariantCultureIgnoreCase))
                        {
                            ClrAssemblyInfo ass = reader.Read(module);

                            foreach (MethodBase m in ass.EnumerateMethods())
                            {
                                yield return m;
                            }

                            yield break;
                        }
                    }
                }

            }//end using
        }

        ClrRuntime runtime;

        public ClrAssemblyReader(ClrRuntime r)
        {
            if (r == null) throw new ArgumentNullException("r");

            this.runtime = r;
        }

        public ClrRuntime SourceRuntime { get { return this.runtime; } }

        public ClrAssemblyInfo Read(string modulename)
        {
            if (modulename == null) modulename = String.Empty;

            ClrModule module = null;

            foreach (ClrModule x in runtime.Modules)
            {
                if (x.FileName == null) continue;

                if (Path.GetFileName(x.FileName).Equals(modulename, StringComparison.InvariantCultureIgnoreCase))
                {
                    module = x;
                }
            }

            if (module == null) return null;

            return (ClrAssemblyInfo)this.Read(module);
        }

        public ClrAssemblyInfo Read(ClrModule module)
        {
            if (module == null) throw new ArgumentNullException("module");

            //get metadata tokens for specified module in ClrMD debugging session
            ClrAssemblyInfo ass = new ClrAssemblyInfo(module);

            foreach (ClrType t in module.EnumerateTypes())
            {
                ClrTypeInfo ti = new ClrTypeInfo(t, ass);
                ass.SetMemberByToken((int)t.MetadataToken, ti);

                foreach (var m in t.Methods)
                {
                    if (!(m.Type.Name == t.Name)) continue; //skip inherited methods

                    ass.SetMemberByToken((int)m.MetadataToken, new ClrMethodInfo(m, ti));
                }

                foreach (var f in t.Fields)
                {
                    ass.SetMemberByToken((int)f.Token, new ClrFieldInfo(f, ti));
                }

                foreach (var f in t.StaticFields)
                {
                    ass.SetMemberByToken((int)f.Token, new ClrFieldInfo(f, ti));
                }

                foreach (var f in t.ThreadStaticFields)
                {
                    ass.SetMemberByToken((int)f.Token, new ClrFieldInfo(f, ti));
                }
            }

            return ass;
        }
    }
}
