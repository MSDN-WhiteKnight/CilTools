using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserTest
{
    class Program
    {
        public static void DoSomething(string s)
        {
            //placeholder method
        }

        public static void TemplateMethod() //template for a dynamic method
        {
            int a = 1;
            int b = 2;
            DoSomething((a).ToString());
            DoSomething("+");
            DoSomething((b).ToString());
            DoSomething("=");
            DoSomething((a + b).ToString());
        }

        static void Main(string[] args)
        {
            //get the list of instruction for template method
            IEnumerable<CilInstruction> instructions = typeof(Program).GetMethod("TemplateMethod").GetInstructions();

            //create two dynamic methods and get their IL Generators
            DynamicMethod dm1 = new DynamicMethod("Method1", typeof(void), new Type[] { }, typeof(Program).Module);
            ILGenerator ilg = dm1.GetILGenerator(256);
            DynamicMethod dm2 = new DynamicMethod("Method2", typeof(void), new Type[] { }, typeof(Program).Module);
            ILGenerator ilg2 = dm2.GetILGenerator(256);

            //copy template method locals into dynamic methods
            foreach (var local in typeof(Program).GetMethod("TemplateMethod").GetMethodBody().LocalVariables)
            {
                ilg.DeclareLocal(local.LocalType);
                ilg2.DeclareLocal(local.LocalType);
            }

            foreach (var ins in instructions)
            {
                Console.WriteLine(ins.ToString());
                
                if ((ins.OpCode.Equals(OpCodes.Call) || ins.OpCode.Equals(OpCodes.Callvirt)) &&
                    ins.ReferencedMember.Name == "DoSomething")
                {
                    //replace every DoSomething call by Console.Write call in first method
                    //and by System.Diagnostics.Debug.Write call in second method

                    ilg.EmitCall(OpCodes.Call, 
                        typeof(Console).GetMethods().Where((x)=>{
                            return x.Name == "Write" && x.GetParameters().Length == 1 &&
                                   x.GetParameters()[0].ParameterType == typeof(string);
                        }).First(), 
                        null);

                    ilg2.EmitCall(OpCodes.Call,
                        typeof(System.Diagnostics.Debug).GetMethods().Where((x) =>
                        {
                            return x.Name == "Write" && x.GetParameters().Length == 1 &&
                                   x.GetParameters()[0].ParameterType == typeof(string);
                        }).First(),
                        null);
                }
                else
                {
                    //copy other instructions as-is
                    ilg.EmitInstruction(ins);
                    ilg2.EmitInstruction(ins);
                }
            }

            //create and execute delegates for both dynamic methods
            Action deleg = (Action)dm1.CreateDelegate(typeof(Action));
            deleg();
            deleg = (Action)dm2.CreateDelegate(typeof(Action));
            deleg();

            Console.ReadKey();
        }

    }
}
