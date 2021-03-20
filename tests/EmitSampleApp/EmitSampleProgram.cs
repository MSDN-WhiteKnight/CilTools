/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleApp
{
    public class EmitSampleProgram
    {
        public static int x=0;

        static void BuildMethod(ILGenerator ilg)
        {
            ilg.BeginExceptionBlock();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stsfld, typeof(EmitSampleProgram).GetField("x"));
            ilg.BeginCatchBlock(typeof(Exception));
            ilg.EndExceptionBlock();
            ilg.Emit(OpCodes.Ldstr, "");
            ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            ilg.Emit(OpCodes.Ldsfld, typeof(EmitSampleProgram).GetField("x"));
            ilg.Emit(OpCodes.Ret);
        }

        static void BuildAssembly()
        {
            //emit dynamic assembly

            AssemblyName aName = new AssemblyName("DynamicAssemblyExample");

            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(
                aName,AssemblyBuilderAccess.Run
                );

            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);
            TypeBuilder tb = mb.DefineType("MyDynamicType",TypeAttributes.Public);

            FieldBuilder fbNumber = tb.DefineField(
                "number",
                typeof(int),
                FieldAttributes.Private|FieldAttributes.Static
            );

            MethodBuilder m = tb.DefineMethod(
                "Method2", MethodAttributes.Public | MethodAttributes.Static,
                typeof(int), new Type[] { typeof(int), typeof(int) }
                );

            ILGenerator ilg = m.GetILGenerator(512);
            BuildMethod(ilg);
            Type t = tb.CreateType(); //finish creating dynamic type
            if (t == null) throw new ApplicationException("CreateType failed");

            //run emitted method from dynamic assembly
            MethodInfo mi = t.GetMethod("Method2");
            var deleg = (Func<int, int, int>)mi.CreateDelegate(typeof(Func<int, int, int>));

            int res = deleg(1, 2);
            Console.WriteLine("MethodBuilder result:");
            Console.WriteLine(res.ToString());
        }

        static void Main(string[] args)
        {
            //test dynamic method

            DynamicMethod dm = new DynamicMethod(
                "Method1", typeof(int), new Type[] { typeof(int), typeof(int) }, typeof(EmitSampleProgram).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);

            BuildMethod(ilg);

            Func<int, int, int> deleg = (Func<int, int, int>)dm.CreateDelegate(typeof(Func<int, int, int>));
            int res = deleg(1,2);
            Console.WriteLine("DynamicMethod result:");
            Console.WriteLine(res.ToString());

            //test dynamic assembly
            BuildAssembly();

            Console.ReadLine();
        }
    }
}
