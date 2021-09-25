/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", MethodSource.All)]
        public void Test_CilGraph_HelloWorld(MethodBase mi)
        {
            //MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraphTestsCore.Test_CilGraph_HelloWorld(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Loop()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintTenNumbers");
            CilGraphTestsCore.Test_CilGraph_Loop(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Exceptions()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphTestsCore.Test_CilGraph_Exceptions(mi);
        }

        [TestMethod]
        public void Test_CilGraph_GetHandlerNodes()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphTestsCore.Test_CilGraph_GetHandlerNodes(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Tokens()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestTokens");
            CilGraphTestsCore.Test_CilGraph_Tokens(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Constrained()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("ConstrainedTest");
            CilGraphTestsCore.Test_CilGraph_Constrained(mi);
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Pointer()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PointerTest");
            CilGraphTestsCore.Test_CilGraph_Pointer(mi);
        }
#endif

        [TestMethod]
        public void Test_CilGraph_DynamicMethod()
        {            
            //create dynamic method
            DynamicMethod dm = new DynamicMethod(
                "DynamicMethodTest", typeof(int), new Type[] { typeof(string) }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(2048);
            ilg.DeclareLocal(typeof(string));

            ilg.BeginExceptionBlock();
            ilg.Emit(OpCodes.Ldstr, "Hello, world.");

            ilg.Emit(OpCodes.Stloc, (short)0);
            ilg.Emit(OpCodes.Ldloc, (short)0);
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) })
                );

            ilg.Emit(OpCodes.Ldsfld, typeof(SampleMethods).GetField("f"));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) })
                );

            ilg.BeginCatchBlock(typeof(Exception));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) })
                );
            
            ilg.BeginFinallyBlock();

            ilg.Emit(OpCodes.Ldc_I4, 10);
            ilg.Emit(OpCodes.Newarr, typeof(Guid));
            ilg.Emit(OpCodes.Pop);
            ilg.EndExceptionBlock();            

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);

            var deleg = (Func<string, int>)dm.CreateDelegate(typeof(Func<string, int>));
            int res = deleg("Hello, world!");

            Console.WriteLine(Environment.Version.ToString());
            Diagnostics.Error += (x, y) => { Console.WriteLine(y.Exception.ToString()); };
            CilGraphTestsCore.Test_CilGraph_DynamicMethod(dm);
        }
    }
}
