/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    internal static class Types
    {
        static readonly Type tDynamicMethod;

        public static Type DynamicMethodType
        {
            get
            {
                return tDynamicMethod;
            }
        }

        static readonly Type tILGenerator = typeof(OpCode).Assembly.GetType("System.Reflection.Emit.ILGenerator");

        public static Type ILGeneratorType
        {
            get
            {
                return tILGenerator;
            }
        }

        static Types()
        {
            Assembly assEmit = typeof(System.Reflection.Emit.OpCode).Assembly;
            tDynamicMethod = assEmit.GetType("System.Reflection.Emit.DynamicMethod");
            tILGenerator = assEmit.GetType("System.Reflection.Emit.ILGenerator");
        }

        public static bool IsDynamicMethod(MethodBase mb)
        {
            if(DynamicMethodType == null) return false;

            return DynamicMethodType.IsAssignableFrom(mb.GetType());
        }
    }
}
