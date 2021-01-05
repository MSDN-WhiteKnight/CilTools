/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilView
{
    public static class Analysis
    {
        static void GetExceptionsRecursive(
            MethodBase m, HashSet<Type> results,HashSet<MethodBase> visited,int c
            )
        {
            //common false positives:
            //System.Threading.Thread get_CurrentCulture (TypeLoadException)
            //ObjectDisposedException

            if (visited.Contains(m)) return;
            visited.Add(m);

            if (String.Equals(m.Name,"Sleep",StringComparison.InvariantCulture) &&
                String.Equals(m.DeclaringType.FullName,"System.Threading.Thread", StringComparison.InvariantCulture) 
                && c>0)
            {
                //Thread.Sleep initiates wait and thus brings up some exceptions it 
                //could not realistically throw, 
                //such as AbandonedMutexException or ObjectDisposedException
                return;
            }

            string debug = m.DeclaringType.ToString() + " " + m.Name + " " + c.ToString();
            System.Diagnostics.Debug.WriteLine(debug);

            if (c > 100000)
            {
                throw new ApplicationException("Error: recursion is too deep in GetExceptions");
            }

            CilGraph graph;

            try
            {
                graph = CilGraph.Create(m);
            }
            catch (TypeLoadException)
            {
                return;
            }

            foreach (CilGraphNode node in graph.GetNodes())
            {
                if (node.Instruction.OpCode == OpCodes.Throw)
                {
                    CilGraphNode prev = node.Previous;
                    if (prev == null) continue;

                    if (prev.Instruction.OpCode == OpCodes.Newobj)
                    {
                        MethodBase constr = prev.Instruction.ReferencedMember as MethodBase;
                        if (constr != null)
                        {
                            bool res = results.Add(constr.DeclaringType);
                            if(res) System.Diagnostics.Debug.WriteLine("Newobj: " + constr.DeclaringType.ToString());
                        }
                    }
                    else if (prev.Instruction.OpCode == OpCodes.Ldloc)
                    {
                        int index = Convert.ToInt32(prev.Instruction.Operand);
                        Type lt = null;

                        if (m is CustomMethod)
                        {
                            CustomMethod cm = (CustomMethod)m;
                            LocalVariable[] locals = cm.GetLocalVariables();
                            if (locals.Length != 0) lt = locals[index].LocalType;
                        }
                        else
                        {
                            lt = prev.Instruction.ReferencedLocal.LocalType;
                        }

                        if (lt != null)
                        {
                            bool res=results.Add(lt);
                            if(res) System.Diagnostics.Debug.WriteLine("Local: " + lt.ToString());
                        }
                    }
                    else if (prev.Instruction.OpCode.FlowControl == FlowControl.Call)
                    {
                        /*MethodBase target = prev.Instruction.ReferencedMember as MethodBase;
                        Type rt = null;

                        if (target != null)
                        {
                            if (target is MethodInfo) rt = ((MethodInfo)target).ReturnType;
                            else if(target is CustomMethod) rt = ((CustomMethod)target).ReturnType;
                        }

                        if (rt != null) results.Add(rt);*/
                    }
                }
                else if (node.Instruction.OpCode.FlowControl == FlowControl.Call)
                {
                    MethodBase target = node.Instruction.ReferencedMember as MethodBase;

                    if (target != null)
                    {
                        GetExceptionsRecursive(target, results,visited,c+1);
                    }
                }//endif
            }//end foreach
        }

        public static IEnumerable<Type> GetExceptions(MethodBase m)
        {
            HashSet<Type> results = new HashSet<Type>();
            HashSet<MethodBase> visited = new HashSet<MethodBase>();
            GetExceptionsRecursive(m, results,visited, 0);
            return results;
        }
    }
}
