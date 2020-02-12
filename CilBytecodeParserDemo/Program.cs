/* CilBytecodeParser library demo application
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserDemo
{
    class Program
    {
        public static int f = 2;

        static void Main(string[] args)
        {
            DynamicMethod dm = new DynamicMethod("Method", typeof(void), new Type[] { }, typeof(Program).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            ilg.Emit(OpCodes.Ldstr, "Hello world");
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) })
                );
            ilg.Emit(OpCodes.Ldsfld,typeof(Program).GetField("f"));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) })
                );
            ilg.Emit(OpCodes.Ldc_I4, 10);
            ilg.Emit(OpCodes.Newarr, typeof(object));
            ilg.Emit(OpCodes.Pop);
            ilg.Emit(OpCodes.Ret);
            var deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();            

            var mb = new CilBytecodeParser.Reflection.MethodBaseWrapper(dm);
            byte[] il = mb.GetBytecode();
            var module = mb.ModuleWrapper;

            var coll = CilReader.GetInstructions(il);

            foreach (CilInstruction instr in coll)
            {
                if (instr.OpCode.OperandType == OperandType.InlineMethod)
                {
                    Console.Write(instr.OpCode.ToString()+" ");
                    var meth = module.ResolveMethod((int)instr.Operand,null,null);
                    if (meth != null) Console.WriteLine(meth.ToString());
                    else Console.WriteLine("<unknown method>");
                }
                else if (instr.OpCode.OperandType == OperandType.InlineField)
                {
                    Console.Write(instr.OpCode.ToString() + " ");
                    var f = module.ResolveField((int)instr.Operand, null, null);
                    if (f != null) Console.WriteLine(f.ToString());
                    else Console.WriteLine("<unknown field>");
                }
                else if (instr.OpCode.OperandType == OperandType.InlineString)
                {
                    Console.Write(instr.OpCode.ToString() + " ");
                    Console.WriteLine(module.ResolveString((int)instr.Operand).ToString());
                }
                else if (instr.OpCode.OperandType == OperandType.InlineType)
                {
                    Console.Write(instr.OpCode.ToString() + " ");
                    var t = module.ResolveType((int)instr.Operand, null, null);
                    if (t != null) Console.WriteLine(t.ToString());
                    else Console.WriteLine("<unknown type>");
                }
                else Console.WriteLine(instr.ToString());
            }

            //Console.ReadKey();
            ;
            return;

            Console.WriteLine("*** CIL Bytecode Parser library demo ***");
            Console.WriteLine("Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) ");
            Console.WriteLine();
            string asspath;
            string type;
            string method;
            Assembly ass;

            try
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Prints CIL code of the specified method");
                    Console.WriteLine("Usage: CilBytecodeParserDemo.exe (assembly path) (type full name) (method name)");
                    Console.WriteLine();
                    Console.WriteLine("When called without arguments, prints yourself");
                    Console.WriteLine();

                    asspath = "";
                    type = "CilBytecodeParserDemo.Program";
                    method = "Main";

                    Console.WriteLine(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);
                    ass = Assembly.GetExecutingAssembly();
                }
                else
                {
                    asspath = args[0];
                    type = args[1];
                    method = args[2];
                    Console.WriteLine(asspath);
                    ass = Assembly.LoadFrom(asspath);
                }

                Console.WriteLine("Method: {0}:{1}", type, method);
                Console.WriteLine();                                

                Type t = ass.GetType(type);

                MethodInfo[] methods = t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );

                MethodInfo mi = methods.Where((x) => { return x.Name == method; }).First();
                CilGraph graph = CilAnalysis.GetGraph(mi);
                
                graph.Print(null, true, true, true, true);                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                /*if (!Console.IsInputRedirected) Console.ReadKey();*/
                throw;
            }

            /*if (!Console.IsInputRedirected) Console.ReadKey();*/            
        }

    }
}
