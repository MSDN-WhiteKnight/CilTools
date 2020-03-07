/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilBytecodeParser;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    class ClrAssemblyInfo:Assembly
    {
        ClrModule module;
        AssemblyName asn;

        public ClrAssemblyInfo(ClrModule m)
        {
            this.module = m;
            AssemblyName n = new AssemblyName();

            if (m != null)
            {

                n.Name = Path.GetFileNameWithoutExtension(this.module.AssemblyName);

                if (this.module.IsFile) n.CodeBase = this.module.FileName;
                else n.CodeBase = "";
            }
            else
            {
                n.Name = "???";
                n.CodeBase = "";
            }

            this.asn = n;
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
                return this.asn.CodeBase;
            }
        }

        public override string CodeBase
        {
            get
            {
                return this.asn.CodeBase;
            }
        }
    }
}
