/* CilTools.Metadata tests
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Tests.Common;

namespace CilTools.Metadata.Tests
{
    static class ReaderFactory
    {
        static readonly AssemblyReader s_globalReader;

        static ReaderFactory()
        {
            s_globalReader = new AssemblyReader();
            s_globalReader.AssemblyResolve += globalReader_AssemblyResolve;
        }

        static Assembly globalReader_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);

            if (an.Name == "CilTools.Tests.Common")
            {
                //manually resolve CilTools.Tests.Common, as it is not resolved automatically under tests runner
                return s_globalReader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            }
            else return null;
        }

        public static AssemblyReader GetReader()
        {
            return s_globalReader;
        }
    }
}
