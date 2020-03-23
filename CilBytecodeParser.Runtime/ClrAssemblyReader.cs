using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilBytecodeParser;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    public class ClrAssemblyReader
    {
        ClrRuntime runtime;

        public ClrAssemblyReader(ClrRuntime r)
        {
            this.runtime = r;
        }

        public Assembly Read(ClrModule module)
        {
            if (module == null) throw new ArgumentNullException("module");

            //get metadata tokens for specified module in ClrMD debugging session
            ClrAssemblyInfo ass = new ClrAssemblyInfo(module);

            foreach (ClrType t in module.EnumerateTypes())
            {
                ClrTypeInfo ti = new ClrTypeInfo(t, ass);
                ass.SetValue((int)t.MetadataToken, ti);

                foreach (var m in t.Methods)
                {
                    if (!(m.Type.Name == t.Name)) continue; //skip inherited methods

                    ass.SetValue((int)m.MetadataToken, new ClrMethodInfo(m, ti));
                }

                foreach (var f in t.Fields)
                {
                    ass.SetValue((int)f.Token, new ClrFieldInfo(f, ti));
                }

                foreach (var f in t.StaticFields)
                {
                    ass.SetValue((int)f.Token, new ClrFieldInfo(f, ti));
                }

                foreach (var f in t.ThreadStaticFields)
                {
                    ass.SetValue((int)f.Token, new ClrFieldInfo(f, ti));
                }
            }

            return ass;
        }
    }
}
