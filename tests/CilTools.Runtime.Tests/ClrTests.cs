/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.Diagnostics.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Runtime.Tests
{
    [LongTest]
    public class ClrTests
    {
        static Process process = null;
        static DataTarget dataTarget = null;
        static ClrRuntime runtime = null;

        public static void StartSession()
        {
            //start test app
            string path = @"..\..\..\EmitSampleApp\bin\Debug\net45\win-x86\EmitSampleApp.exe";
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = path;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process pr = Process.Start(psi);

            //wait for init
            Thread.Sleep(2000);

            //attach to process via ClrMD
            DataTarget dt = DataTarget.AttachToProcess(pr.Id, 5000, AttachFlag.NonInvasive);

            process = pr;
            dataTarget = dt;

            //pick first .NET runtime in the target process
            ClrInfo runtimeInfo = dataTarget.ClrVersions[0];
            runtime = runtimeInfo.CreateRuntime();
        }

        static ClrAssemblyReader GetReader()
        {
            ClrAssemblyReader reader = new ClrAssemblyReader(runtime);
            return reader;
        }

        public static void CloseSession()
        {
            if (dataTarget != null) dataTarget.Dispose();

            if (process != null)
            {
                process.Kill();
                process.Dispose();
            }
        }

        [LongTest]
        public void TestDynamicMethod()
        {
            ClrAssemblyReader reader = GetReader();
            DynamicMethodsAssembly ass = reader.GetDynamicMethods();
            Type t = ass.ChildType;

            //get dynamic method
            MethodBase mb = (MethodBase)(t.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                )[0]);

            string str = CilAnalysis.MethodToText(mb);

            AssertThat.IsMatch(str, new Text[] {
                     new Literal(".method"), Text.Any,
                     new Literal("Method1"), Text.Any,
                     new Literal("cil"), Text.Any,
                     new Literal("managed"), Text.Any,
                     new Literal(".try"), Text.Any,
                     new Literal("catch"), Text.Any,
                     new Literal("System.Exception"), Text.Any,
                     new Literal("call"), Text.Any,
                     new Literal("System.Console::WriteLine"), Text.Any,
                     new Literal("ret"), Text.Any
             });
        }

        /*
        .method   public static class [???]??? Method1() cil managed
        {

         .try
         {
                   ldarg.0      
                   ldarg.1      
                   add          
                   stsfld       UnknownField4000002
                   leave        IL_0001
         }
         catch [mscorlib]System.Exception
         {
          IL_0001: leave        IL_0002
         }
         IL_0002: ldstr        UnknownString70000004
                  call         void [mscorlib]System.Console::WriteLine(string)
                  ldsfld       UnknownField4000006
                  ret          
        }
        */

        [LongTest]
        public void Test_DynamicAssembly_Method()
        {
            ClrAssemblyReader reader = GetReader();

            //get dynamic assembly
            ClrModule module = runtime.Modules.Where(x => x.IsDynamic == true).First();
            ClrAssemblyInfo ass = reader.Read(module);

            //get method
            Type t = ass.GetType("MyDynamicType");
            MethodBase mb = (MethodBase)(t.GetMember("Method2")[0]);

            //test instructions
            CilInstruction[] instructions = CilReader.GetInstructions(mb).ToArray();

            AssertThat.HasOnlyOneMatch(
                instructions,
                x => x.OpCode.Equals(OpCodes.Add)
                );

            AssertThat.HasOnlyOneMatch(
                instructions,
                x => x.OpCode.Equals(OpCodes.Call)
                );

            AssertThat.HasOnlyOneMatch(
                instructions,
                x => x.OpCode.Equals(OpCodes.Ldstr)
                );

            AssertThat.HasOnlyOneMatch(
                instructions,
                x => x.OpCode.Equals(OpCodes.Ldsfld)
                );

            AssertThat.HasOnlyOneMatch(
                instructions,
                x => x.OpCode.Equals(OpCodes.Stsfld)
                );

            Assert.IsTrue(instructions.Last().OpCode.Equals(OpCodes.Ret));

            //test disassembler
            string str = CilAnalysis.MethodToText(mb);

            AssertThat.IsMatch(str, new Text[] {
                     new Literal(".method"), Text.Any,
                     new Literal("Method2"), Text.Any,
                     new Literal("cil"), Text.Any,
                     new Literal("managed"), Text.Any,
                     new Literal(".try"), Text.Any,
                     new Literal("add"), Text.Any,
                     new Literal("stsfld"), Text.Any,
                     new Literal("catch"), Text.Any,
                     new Literal("System.Exception"), Text.Any,
                     new Literal("ldstr"), Text.Any,
                     new Literal("call"), Text.Any,
                     new Literal("ldsfld"), Text.Any,
                     new Literal("ret"), Text.Any
             });

            /*
            .method  public static class [???]??? Method2() cil managed
            {

             .try
             {
                       ldarg.0      
                       ldarg.1      
                       add          
                       stsfld       UnknownFieldA000001
                       leave        IL_0001
             }
             catch [mscorlib]System.Exception
             {
              IL_0001: leave        IL_0002
             }
             IL_0002: ldstr        UnknownString70000005
                      call         UnknownMethodA000002
                      ldsfld       UnknownFieldA000001
                      ret          
            } 
            */
        }

        [LongTest]
        public void Test_DynamicAssembly_Field()
        {
            ClrAssemblyReader reader = GetReader();

            //get dynamic assembly
            ClrModule module = runtime.Modules.Where(x => x.IsDynamic == true).First();
            ClrAssemblyInfo ass = reader.Read(module);

            //get field
            Type t = ass.GetType("MyDynamicType");

            FieldInfo fi = t.GetField(
                "number",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                );

            Assert.IsNotNull(fi, "MyDynamicType should have field 'number'");
            Assert.AreEqual(fi.FieldType.Name, "Int32");
        }

        [LongTest]
        public void Test_DynamicAssembly_Name()
        {
            ClrAssemblyReader reader = GetReader();

            //get dynamic assembly
            ClrModule module = runtime.Modules.Where(x => x.IsDynamic == true).First();
            ClrAssemblyInfo ass = reader.Read(module);

            //verify dynamic assembly short name
            Assert.AreEqual(
                "DynamicAssemblyExample (dynamic)",
                ass.GetName().Name,
                false,
                System.Globalization.CultureInfo.InvariantCulture
                );

            //dynamic assembly file path should be empty
            Assert.AreEqual(String.Empty, ass.Location);
        }

        [LongTest]
        public void Test_MemoryImage()
        {
            ClrAssemblyReader reader = GetReader();

            ClrModule module = runtime.Modules.Where(
                x => x.IsFile && x.Name.EndsWith("EmitSampleApp.exe", StringComparison.OrdinalIgnoreCase)
                ).First();

            MemoryImage img = ClrAssemblyReader.GetMemoryImage(module);
            Assert.IsNotNull(img);
            Assert.AreEqual(module.Size, (ulong)img.Image.Length);
            Assert.AreEqual(module.FileName, img.FilePath);
            Assert.IsFalse(img.IsFileLayout);
            AssertThat.HasAtLeastOneMatch(img.Image, x => x != 0);
        }
    }
}
