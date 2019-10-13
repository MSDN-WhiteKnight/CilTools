/* CilBytecodeParser library example: generating dynamic methods using existing method as template
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
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
            Random rnd = new Random();
            for (int i = 0; i < 3; i++)
            {
                try
                {                    
                    int a = rnd.Next(0, 9);
                    int b = rnd.Next(10, 15);
                    DoSomething((a).ToString());
                    DoSomething("+");
                    DoSomething((b).ToString());
                    DoSomething("=");                    
                    DoSomething((a + b).ToString());                   
                    
                }
                catch (Exception ex)
                {
                    DoSomething(ex.Message);
                }
                finally
                {
                    DoSomething(";\r\n");
                }
            }
        }  

        static void Main(string[] args)
        {    
            
            //create two dynamic methods and get their IL Generators
            DynamicMethod dm1 = new DynamicMethod("Method1", typeof(void), new Type[] { }, typeof(Program).Module);
            ILGenerator ilg = dm1.GetILGenerator(512);            
            DynamicMethod dm2 = new DynamicMethod("Method2", typeof(void), new Type[] { }, typeof(Program).Module);
            ILGenerator ilg2 = dm2.GetILGenerator(512);

            //build CIL graph for template method
            CilGraph graph = typeof(Program).GetMethod("TemplateMethod").GetCilGraph();
            Console.WriteLine(graph);
            
            //emit two dynamic methods based on the template
            graph.EmitTo(ilg, (instr) => {   
                if ((instr.OpCode.Equals(OpCodes.Call) || instr.OpCode.Equals(OpCodes.Callvirt)) &&
                    instr.ReferencedMember.Name == "DoSomething")
                {
                    //replace every DoSomething call by Console.Write call

                    ilg.EmitCall(OpCodes.Call,
                        typeof(Console).GetMethods().Where((x) =>
                        {
                            return x.Name == "Write" && x.GetParameters().Length == 1 &&
                                   x.GetParameters()[0].ParameterType == typeof(string);
                        }).First(),
                        null);
                    return true; //handled
                }
                else return false;
            });

            graph.EmitTo(ilg2, (instr) => {     
                if ((instr.OpCode.Equals(OpCodes.Call) || instr.OpCode.Equals(OpCodes.Callvirt)) &&
                    instr.ReferencedMember.Name == "DoSomething")
                {
                    //replace every DoSomething call by System.Diagnostics.Debug.Write call

                    ilg2.EmitCall(OpCodes.Call,
                        typeof(System.Diagnostics.Debug).GetMethods().Where((x) =>
                        {
                            return x.Name == "Write" && x.GetParameters().Length == 1 &&
                                   x.GetParameters()[0].ParameterType == typeof(string);
                        }).First(),
                        null);
                    return true; //handled
                }
                else return false;
            });
            
            //create and execute delegates for both dynamic methods
            Action deleg = (Action)dm1.CreateDelegate(typeof(Action));
            deleg();
            deleg = (Action)dm2.CreateDelegate(typeof(Action));
            deleg();

            Console.ReadKey();
        }
    }
}
