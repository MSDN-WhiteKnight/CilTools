/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    public sealed class DynamicMethodsAssembly : Assembly,IDisposable
    {
        DataTarget target; //null indicates that object is disposed
        AssemblyName asn;
        DynamicMethodsType type;
        bool ownsTarget; //if true, automatically dispose DataTarget on Dispose()

        public DynamicMethodsAssembly(DataTarget dt, bool autoDispose)
        {
            if (dt == null) throw new ArgumentNullException("dt");

            this.target = dt;
            this.ownsTarget = autoDispose;
            this.type = new DynamicMethodsType(this);
            AssemblyName n = new AssemblyName();            
            n.Name = "<Process"+dt.ProcessId.ToString()+".DynamicMethods>";
            n.CodeBase = "";
            this.asn = n;
        }

        public Type ChildType { get { return this.type; } }

        public DataTarget Target
        {
            get { return this.target; }
        }

        public override string FullName
        {
            get
            {
                return this.asn.FullName;
            }
        }

        public override AssemblyName GetName()
        {
            return this.asn;
        }

        public override string Location
        {
            get
            {
                return "";
            }
        }

        public override string CodeBase
        {
            get
            {
                return "";
            }
        }

        public override bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        public override bool ReflectionOnly { get { return true; } }

        public IEnumerable<MethodBase> EnumerateMethods()
        {
            if (this.target == null) throw new ObjectDisposedException("DataTarget");

            foreach (ClrInfo runtimeInfo in target.ClrVersions)
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
                        ClrDynamicMethod dm = new ClrDynamicMethod(o,this);
                        yield return dm;
                    }
                }
            }
        }

        public override Type[] GetTypes()
        {
            return new Type[] { this.type };
        }

        public override Type GetType(string name)
        {
            return GetType(name, false, false);
        }

        public override Type GetType(string name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            StringComparison comp;

            if (ignoreCase) comp = StringComparison.InvariantCultureIgnoreCase;
            else comp = StringComparison.InvariantCulture;

            if (String.Equals(name, DynamicMethodsType.TypeName, comp))
            {
                return this.type;
            }
            else
            {
                if (throwOnError) throw new TypeLoadException("Type " + name + " not found");
                else return null;
            }
        }

        public override IEnumerable<Type> ExportedTypes
        {
            get
            {
                return this.GetExportedTypes();
            }
        }

        public override Type[] GetExportedTypes()
        {
            return new Type[] { };
        }

        public void Dispose()
        {
            if (this.target == null) return; //already disposed

            //dispose underlying data target, if we own it

            if (this.ownsTarget)
            {
                this.target.Dispose();
            }

            this.target = null;
        }
    }
}
