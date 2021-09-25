/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilReaderTests
    {
        [TestMethod]
        public void Test_CilReader_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilReaderTestsCore.Test_CilReader_HelloWorld(mi);

            //Test EmitTo: only NetFX
#if NETFRAMEWORK
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod(
                "PrintHelloWorldDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }

        [TestMethod]
        public void Test_CilReader_CalcSum()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("CalcSum");
            CilReaderTestsCore.Test_CilReader_CalcSum(mi);
        }

        [TestMethod]
        public void Test_CilReader_StaticFieldAccess()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("SquareFoo");
            CilReaderTestsCore.Test_CilReader_StaticFieldAccess(mi);

            //Test EmitTo: only NetFX
#if NETFRAMEWORK
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod("SquareFooDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
            Assert.AreEqual(4, SampleMethods.Foo, "The value of SampleMethods.Foo is wrong");
#endif

        }

        [TestMethod]
        public void Test_CilReader_VirtualCall()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("GetInterfaceCount");
            CilReaderTestsCore.Test_CilReader_VirtualCall(mi);

            //Test EmitTo: only NetFX
            //Dont' run in debug, because compiler generates branching here for some reason
#if NETFRAMEWORK && !DEBUG
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod(
                "GetInterfaceCountDynamic", typeof(int), new Type[] {typeof(Type) }, typeof(SampleMethods).Module
            );
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.DeclareLocal(typeof(Type[]));
            
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            var deleg = (Func<Type, int>)dm.CreateDelegate(typeof(Func<Type, int>));
            int res = deleg(typeof(List<>));
            Assert.AreEqual(SampleMethods.GetInterfaceCount(typeof(List<>)), res, "The result of GetInterfaceCountDynamic is wrong");
#endif
        }

        [TestMethod]
        public void Test_CilReader_GenericType()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintList");
            CilReaderTestsCore.Test_CilReader_GenericType(mi);

            //Test EmitTo: only NetFX            
#if NETFRAMEWORK
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod(
                "PrintListDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.DeclareLocal(typeof(List<string>));

            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }

        [TestMethod]
        public void Test_CilReader_GenericParameter()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("GenerateArray");
            CilReaderTestsCore.Test_CilReader_GenericParameter(mi);
        }

#if NETFRAMEWORK
        [TestMethod]
        public void Test_CilReader_ExternalAssemblyAccess()
        {
            //Disabled on .NET Core: method GetExtension has multiple overloads 
            //now, so this lookup crashes with ambigous match error.
            MethodInfo mi = typeof(System.IO.Path).GetMethod("GetExtension");
            CilReaderTestsCore.Test_CilReader_ExternalAssemblyAccess(mi);
        }
#endif

        [TestMethod]
        public void Test_CilReader_CanIterateTwice()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilReaderTestsCore.Test_CilReader_CanIterateTwice(mi);
        }
    }
}
