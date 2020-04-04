/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SampleApp
{
    class EmitSampleProgram
    {
        public static int x;

        static void Main(string[] args)
        {
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("Method1", typeof(int), new Type[] { typeof(int), typeof(int) }, typeof(EmitSampleProgram).Module);
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.BeginExceptionBlock();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stsfld, typeof(EmitSampleProgram).GetField("x"));
            ilg.BeginCatchBlock(typeof(Exception));
            ilg.EndExceptionBlock();
            ilg.Emit(OpCodes.Ldsfld, typeof(EmitSampleProgram).GetField("x"));
            ilg.Emit(OpCodes.Ret);

            Func<int, int, int> deleg = (Func<int, int, int>)dm.CreateDelegate(typeof(Func<int, int, int>));
            int res = deleg(1,2);
            Console.WriteLine(res.ToString());
#endif
            Console.ReadLine();
        }
    }
}
