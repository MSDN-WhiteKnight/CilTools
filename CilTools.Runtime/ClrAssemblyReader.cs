﻿/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    /// <summary>
    /// Reads information about assemblies in the CLR isntances of external processes using ClrMD
    /// </summary>
    public class ClrAssemblyReader
    {
        ClrRuntime runtime;
        Dictionary<ulong, ClrAssemblyInfo> cache = new Dictionary<ulong, ClrAssemblyInfo>();
        Dictionary<string, Assembly> preloaded = new Dictionary<string, Assembly>(50);
        DynamicMethodsAssembly dynamicMethodsAssembly = null;
        Dictionary<MethodId, ClrDynamicMethod> dynAssMethods = null;
        Dictionary<ulong, string> dynModuleNames = null;
        MethodBase[] dynMethods = null;

        /// <summary>
        /// Gets the pseudo-assembly that represents the collection of dynamic methods in the specified process
        /// </summary>
        /// <param name="process">Source process</param>
        public static DynamicMethodsAssembly GetDynamicMethods(Process process)
        {
            if (process == null) throw new ArgumentNullException("process");

            DataTarget dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);
            DynamicMethodsAssembly ass = new DynamicMethodsAssembly(dt,true);
            return ass;
        }

        /// <summary>
        /// Gets the collection of all methods in the specified process
        /// </summary>
        /// <param name="process">Source process</param>
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
                DynamicMethodsAssembly dynass = new DynamicMethodsAssembly(dt,false);
                var en = dynass.EnumerateMethods();

                foreach (var o in en)
                {
                    yield return o;
                }

            }//end using
        }

        /// <summary>
        /// Gets the collection of all methods defined in the specified module of the process
        /// </summary>
        /// <param name="process">Source process</param>
        /// <param name="modulename">Name of PE module file (without full path), or an empty string to use main module</param>
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

        /// <summary>
        /// Creates <c>ClrAssemblyReader</c> to read information from the specified CLR instance
        /// </summary>
        /// <param name="r">Source CLR instance</param>
        public ClrAssemblyReader(ClrRuntime r)
        {
            if (r == null) throw new ArgumentNullException("r");

            this.runtime = r;
        }

        /// <summary>
        /// Gets the CLR instance associated with this <c>ClrAssemblyReader</c>
        /// </summary>
        public ClrRuntime SourceRuntime { get { return this.runtime; } }

        /// <summary>
        /// Gets the assembly information object for the specified module name
        /// </summary>
        /// <param name="modulename">Name of PE module file (without full path)</param>
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

            return this.Read(module);
        }

        /// <summary>
        /// Gets the assembly information object for the specified ClrMD module object
        /// </summary>
        /// <param name="module">ClrMD module object</param>
        public ClrAssemblyInfo Read(ClrModule module)
        {
            if (module == null) throw new ArgumentNullException("module");
            
            //if assembly was already loaded, return assembly from cache
            if (this.cache.ContainsKey(module.AssemblyId)) return this.cache[module.AssemblyId];

            //load assembly and store it in cache
            ClrAssemblyInfo ret = ReadImpl(module);
            this.cache[module.AssemblyId] = ret;
            return ret;
        }

        ClrAssemblyInfo ReadImpl(ClrModule module)
        {
            //get metadata tokens for specified module in ClrMD debugging session
            ClrAssemblyInfo ass = new ClrAssemblyInfo(module,this);

            foreach (ClrType t in module.EnumerateTypes())
            {
                ClrTypeInfo ti = new ClrTypeInfo(t, ass);
                ass.SetMemberByToken((int)t.MetadataToken, ti);

                foreach (var m in t.Methods)
                {
                    if (m.Type != null)
                    {
                        if (m.Type.Name != t.Name) continue; //skip inherited methods
                    }

                    ass.SetMemberByToken((int)m.MetadataToken, ClrMethodInfo.CreateMethod(m, ti));
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

        /// <summary>
        /// Gets the pseudo-assembly that represents the collection of dynamic methods in the process 
        /// containing the CLR instance
        /// </summary>
        public DynamicMethodsAssembly GetDynamicMethods()
        {
            if (this.dynamicMethodsAssembly == null)
            {
                this.dynamicMethodsAssembly = new DynamicMethodsAssembly(this.runtime.DataTarget, this, false);
            }

            return this.dynamicMethodsAssembly;
        }

        /// <summary>
        /// Adds the specified assembly into the preloaded assembly collection
        /// </summary>
        /// <param name="ass">Preloaded assembly instance</param>
        /// <remarks>
        /// Adding preloaded assemblies enables some <c>CilTools.Runtime</c> mechanisms to avoid 
        /// expensive <c>AssemblyReader.Read</c> calls. The preloaded assembly must implement 
        /// <see cref="CilTools.Reflection.ITokenResolver"/> interface to be useful.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Assembly is null</exception>
        public void AddPreloadedAssembly(Assembly ass)
        {
            if (ass == null) throw new ArgumentNullException("ass");
            string key = ass.GetName().Name;
            if (String.IsNullOrEmpty(key)) return;

            this.preloaded[key] = ass;
        }

        /// <summary>
        /// Removes all assemblies from the preloaded assembly collection
        /// </summary>
        public void ClearPreloadedAssemblies()
        {
            this.preloaded.Clear();
        }

        internal Assembly GetPreloadedAssembly(string name)
        {
            Assembly ass = null;

            if (this.preloaded.TryGetValue(name, out ass))
            {
                return ass; //return preloaded assembly
            }
            else
            {
                return null; //not found
            }
        }

        internal ITokenResolver GetResolver(ClrModule module)
        {
            ITokenResolver ret;

            //try preloaded
            Assembly ass = this.GetPreloadedAssembly(Path.GetFileNameWithoutExtension(module.Name));
            ret = ass as ITokenResolver;
            if (ret != null) return ret;

            //if not found, read via ClrMD
            return this.ReadImpl(module);
        }

        void ProcessDynamicModule(ClrObject o /*ModuleBuilder*/,Dictionary<ulong,string> names) 
        {
            ClrObject obj;
            ClrObject objAssemblyBuilder;
            obj = GetModuleData(o);
            if(obj.IsNull) return;

            long pData = obj.GetField<long>("m_pData");
            if (pData == 0) return;

            if (o.Type.GetFieldByName("m_assemblyBuilder") != null)
            {
                //.NET Framework
                objAssemblyBuilder = o.GetObjectField("m_assemblyBuilder");
            }
            else
            {
                //.NET Core 3.1+
                objAssemblyBuilder = o.GetObjectField("_assemblyBuilder");
            }

            if (objAssemblyBuilder.IsNull) return;
            
            if (objAssemblyBuilder.Type.GetFieldByName("m_assemblyData") != null)
            {
                //.NET Framework
                obj = objAssemblyBuilder.GetObjectField("m_assemblyData");
            }
            else
            {
                //.NET Core 3.1+
                obj = objAssemblyBuilder.GetObjectField("_assemblyData");
            }

            if (obj.IsNull) return;
            
            string name=String.Empty;

            if (obj.Type.GetFieldByName("m_strAssemblyName") != null)
            {
                name = obj.GetStringField("m_strAssemblyName");
            }
            else
            {
                /*AssemblyBuilder: _internalAssemblyBuilder (InternalAssemblyBuilder)
                  InternalAssemblyBuilder: m_fullname (string)*/
            }

            if (String.IsNullOrEmpty(name)) return;

            //store the mapping between module address and its name in dictionary
            names[(ulong)pData] = name;
        }

        static ClrObject GetModuleData(ClrObject objModule) 
        {
            if (objModule.IsNull) return new ClrObject();

            ClrObject objModuleData = new ClrObject();

            if (objModule.Type.GetFieldByName("m_internalModuleBuilder") != null)
            {
                //.NET Framework
                objModuleData = objModule.GetObjectField("m_internalModuleBuilder");
            }
            else
            {
                //.NET Core 3.1+
                objModuleData = objModule.GetObjectField("_internalModuleBuilder");
            }

            return objModuleData;
        }
        
        internal MethodBase[] GetDynamicMethodsArray()
        {
            if (this.dynMethods == null) this.ScanHeap();

            return this.dynMethods;
        }

        void ScanHeap()
        {
            Dictionary<MethodId, ClrDynamicMethod> ret = new Dictionary<MethodId, ClrDynamicMethod>();
            Dictionary<ulong, string> moduleNames = new Dictionary<ulong, string>();
            List<MethodBase> dms = new List<MethodBase>(50);
            DynamicMethodsAssembly dma = this.GetDynamicMethods();
            ClrDynamicMethod dm;

            //scan GC heap for dynamic modules and methods
            HeapScanner scanner = new HeapScanner();

            //dynamic module
            scanner.RegisterAction(
                "System.Reflection.Emit.ModuleBuilder", 
                (o) => { ProcessDynamicModule(o, moduleNames);}
                );

            //dynamic method
            scanner.RegisterAction(
                "System.Reflection.Emit.DynamicMethod",
                (o) => {
                    dm = new ClrDynamicMethod(o, dma);
                    dms.Add(dm);
                });

            //method in dynamic assembly
            scanner.RegisterAction(
                "System.Reflection.Emit.MethodBuilder",
                (o) => {
                    //   Objects chain:
                    // MethodBuilder.m_module (ModuleBuilder)
                    // ModuleBuilder.m_assemblyBuilder (AssemblyBuilder)
                    // System.Reflection.Emit.AssemblyBuilder.m_assemblyData (System.Reflection.Emit.AssemblyBuilderData)
                    // AssemblyBuilderData.m_strAssemblyName (string)

                    //get dynamic module
                    ClrObject objModule = o.GetObjectField("m_module");
                    if (objModule.IsNull) return;

                    ClrObject objModuleData = GetModuleData(objModule);

                    if (objModuleData.IsNull) return;

                    //get dynamic module address
                    long pData = objModuleData.GetField<long>("m_pData");
                    if (pData == 0) return;

                    //get method token
                    int token = 0;

                    if (o.Type.GetFieldByName("m_tkMethod") != null)
                    {
                        ClrValueClass cvc_tkMethod = o.GetValueClassField("m_tkMethod");
                        string tokenField = null;

                        if (cvc_tkMethod.Type != null)
                        {
                            //Get the single field of MethodToken struct
                            //(its name varies between runtimes)
                            tokenField = Utils.GetInstanceFields(cvc_tkMethod).FirstOrDefault();
                        }

                        if (tokenField != null)
                        {
                            //.NET Framework or .NET Core 3.1
                            token = cvc_tkMethod.GetField<int>(tokenField);
                        }
                        else
                        {
                            //.NET 5+
                            token = o.GetField<int>("m_token");
                        }
                    }
                    else
                    {
                        //.NET 5+
                        token = o.GetField<int>("m_token");
                    }

                    if (token == 0) return;

                    MethodId mid = new MethodId((ulong)pData, token);
                    dm = new ClrDynamicMethod(o, dma);
                    ret[mid] = dm;
                });

            //run the scanning process
            scanner.ScanHeap(this.runtime.DataTarget);
            
            //handle results
            this.dynAssMethods = ret; //methods in dynamic assemblies
            this.dynModuleNames = moduleNames; //dynamic module names
            this.dynMethods = dms.ToArray(); //dynamic methods (not in assembly)
        }

        internal ClrDynamicMethod GetDynamicAssemblyMethod(MethodId mid)
        {
            if (this.dynAssMethods == null)
            {
                this.ScanHeap();
            }

            ClrDynamicMethod ret;

            if (this.dynAssMethods.TryGetValue(mid, out ret))
            {
                return ret;
            }
            else
            {
                return null;
            }
        }

        internal ClrDynamicMethod GetDynamicAssemblyMethod(ulong module, int token)
        {
            return this.GetDynamicAssemblyMethod(new MethodId(module, token));
        }

        internal string GetDynamicModuleName(ulong module) 
        {
            if (this.dynModuleNames == null)
            {
                this.ScanHeap();
            }

            if (this.dynModuleNames == null) return null;

            string ret;

            if (this.dynModuleNames.TryGetValue(module, out ret))
            {
                return ret;
            }
            else
            {
                return null;
            }
        }

        static byte[] ReadMemoryIgnoreErrors(DataTarget dt, ulong address, int size)
        {
            //Reads the specified memory region, ignoring any uncommitted or inaccessible pages.
            //This is needed because loaded PE image has uncommitted "holes" in it due to 
            //a section alignment.
            byte[] ret = new byte[size];
            int count_errors = 0;
            int page_size = Environment.SystemPageSize;
            byte[] buffer = new byte[page_size];
            int c;

            for (int i = 0; i < size; i += page_size)
            {
                if (i + page_size > size) break;

                bool res = dt.ReadProcessMemory(address + (ulong)i, buffer, page_size, out c);

                if (res == false || c < page_size) count_errors += page_size;
                else Array.Copy(buffer, 0, ret, i, page_size);
            }

            if (count_errors >= size)
            {
                Debug.WriteLine("ReadMemoryIgnoreErrors failed");
                return new byte[0];//couldn't read anything
            }

            return ret;
        }

        /// <summary>
        /// Gets the executable image data for the specified module
        /// </summary>
        /// <param name="module">The ClMD module object</param>
        /// <remarks>
        /// This method gets the executable image data loaded into the target process. This enables you 
        /// to inspect that image with PE reader libraries without the extra cost of reading the image from
        /// file. The method can fail if the memory pages where the image is stored are inaccessible. 
        /// It also does not work for memory-only dynamic modules that don't have the backing PE image.
        /// </remarks>
        /// <returns>In-memory executable image data, or <c>null</c> if the operation failed</returns>
        /// <exception cref="ArgumentNullException">module is <c>null</c></exception>
        public static MemoryImage GetMemoryImage(ClrModule module)
        {
            if (module == null) throw new ArgumentNullException("module");
            if (module.Size == 0) return null;
            
            byte[] imageBytes = new byte[module.Size];
            DataTarget dt = module.Runtime.DataTarget;

            string path = String.Empty;
            if (module.IsFile) path = module.FileName;
            
            int c = 0;
            bool res=dt.ReadProcessMemory(module.ImageBase, imageBytes, (int)module.Size, out c);

            if (res == false)
            {
                //Failed to read memory
                Debug.WriteLine("ReadProcessMemory failed. Module: " + path);
                return null;
            }

            if ((ulong)c < module.Size)
            {
                //Partial read. Try to read ignoring errors.
                
                imageBytes = ReadMemoryIgnoreErrors(dt, module.ImageBase, (int)module.Size);

                if (imageBytes.Length == 0)
                {
                    Debug.WriteLine("ReadProcessMemory partial read. Module: " + path);
                    return null;
                }
            }

            MemoryImage img = new MemoryImage(imageBytes, path, false);
            return img;
        }
    }
}
