﻿/* CIL Tools 
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
    public class ExceptionInfo
    {
        Type type;
        string stack;

        public ExceptionInfo(Type t, string st)
        {
            this.type = t;
            this.stack = st;
        }

        public Type ExceptionType { get { return this.type; } }

        public string Callstack { get { return this.stack; } }

        class GetExceptionsRecursive_Context
        {
            public Dictionary<Type, ExceptionInfo> results;
            public HashSet<MethodBase> visited;
            public Stack<string> callstack=new Stack<string>();
            public MethodBase first;
        }

        class MethodComparer : IEqualityComparer<MethodBase>
        {
            public bool Equals(MethodBase x, MethodBase y)
            {
                if (x == null)
                {
                    if (y == null) return true;
                    else return false;
                }
                else if (y == null)
                {
                    return false;
                }

                Assembly ass_x = null;
                Assembly ass_y = null;
                int token_x=x.MetadataToken;
                int token_y=y.MetadataToken;

                Type t = x.DeclaringType;

                if (t != null)
                {
                    ass_x = t.Assembly;
                }

                t = y.DeclaringType;

                if (t != null)
                {
                    ass_y = t.Assembly;
                }

                return ass_x == ass_y && token_x == token_y;
            }

            public int GetHashCode(MethodBase obj)
            {
                int res = 0;

                if (obj != null)
                {
                    res = obj.MetadataToken;
                }

                return res;
            }
        }

        static bool EqualsInvariant(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCulture);
        }

        static bool SameType(MethodBase left, MethodBase right)
        {
            Type t1 = left.DeclaringType;
            Type t2 = right.DeclaringType;

            if (t1 == null || t2==null)
            {
                return false;
            }

            return EqualsInvariant(t1.FullName, t2.FullName);
        }

        static bool AddTypeToResults(
            Type t,string stackstr,MethodBase m, GetExceptionsRecursive_Context ctx, int c
            )
        {
            //exclude some common false positives
            
            if ((EqualsInvariant(t.FullName, "System.ArgumentOutOfRangeException") ||
                EqualsInvariant(t.FullName, "System.ArgumentNullException") ||
                EqualsInvariant(t.FullName, "System.ArgumentException") ||
                EqualsInvariant(t.FullName, "System.IndexOutOfRangeException") ||
                EqualsInvariant(t.FullName, "System.FormatException")
                ) && c>0 && !SameType(m,ctx.first))
            {
                //Exceptions that indicate incorrect arguments are excluded when they are thrown not
                //directly in the examined method or other method of the same class.
                //They give a lot of false positives when 
                //library function is invoked with hardcoded correct arguments.
                return false;
            }

            if (EqualsInvariant(t.FullName, "System.InvalidOperationException") && 
                c > 0 && !SameType(m, ctx.first))
            {
                //InvalidOperationException indicate that method was called in the wrong 
                //state of object. When method calls external API, it typically
                //ensured that object's state is correct.
                return false;
            }

            if (EqualsInvariant(t.FullName, "System.ObjectDisposedException") && c > 0)
            {
                //ObjectDisposedException pops up every time unmanaged resources are involved,  
                //but in practice it rarely happens
                return false;
            }

            if (EqualsInvariant(t.FullName, "System.OutOfMemoryException") && c > 0)
            {
                //OutOfMemoryException can happen any time we allocate memory.  
                //Excluded, unless directly thrown by this method.
                return false;
            }

            //-----------------------------------

            if (!ctx.results.ContainsKey(t))
            {
                ctx.results[t] = new ExceptionInfo(t, stackstr);
                return true;
            }
            else return false;
        }

        static bool IsExcluded(MethodBase m, int c)
        {
            //exclude some common false positives

            if (EqualsInvariant(m.Name, "Sleep") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Threading.Thread")
                && c > 0)
            {
                //Thread.Sleep initiates wait and thus brings up some exceptions it 
                //could not realistically throw, 
                //such as AbandonedMutexException or ObjectDisposedException
                return true;
            }

            if (EqualsInvariant(m.Name, "GetWinRTResourceManager") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Resources.ResourceManager")
                && c > 0)
            {
                //Tries to load System.Resources.WindowsRuntimeResourceManager type  
                //(System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089), 
                //which brings up TypeLoadException. But it could only really happen when a bug 
                //in runtime packages causes WinRT-related code to be executed on wrong OS
                return true;
            }

            if (EqualsInvariant(m.Name, ".ctor") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Resources.ResourceManager")
                && c > 0)
            {
                //Uses reflection to check attributes, and brings up exceptions like 
                //System.MissingMethodException, which in practice don't happen
                return true;
            }

            if (EqualsInvariant(m.Name, "get_CurrentCulture") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Threading.Thread")
                && c > 0)
            {
                //Thread.CurrentCulture brings up System.Globalization.CultureNotFoundException
                //which does not happen, because current culture surely exists
                return true;
            }

            if (EqualsInvariant(m.Name, "get_CurrentUICulture") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Threading.Thread")
                && c > 0)
            {
                //Thread.CurrentUICulture - same issue as Thread.CurrentCulture above 
                return true;
            }

            if (EqualsInvariant(m.Name, "get_CurrentCulture") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Globalization.CultureInfo")
                && c > 0)
            {
                //CultureInfo.CurrentCulture - same issue as Thread.CurrentCulture above 
                return true;
            }

            if (EqualsInvariant(m.Name, "get_UserDefaultUICulture") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Globalization.CultureInfo")
                && c > 0)
            {
                //CultureInfo.get_UserDefaultUICulture - same issue as Thread.CurrentCulture above 
                return true;
            }

            if (EqualsInvariant(m.Name, "ParseTargetFrameworkName") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.AppContextDefaultValues")
                && c > 0)
            {
                //ParseTargetFrameworkName brings up some exceptions, such as OverflowException, 
                //but they could only happen in practice when AppDomainSetup provides bad value for 
                //target framework. As we rarely work with custom AppDomainSetup's, this is excluded
                return true;
            }

            if (EqualsInvariant(m.Name, "InitializeSourceInfo") &&
                EqualsInvariant( m.DeclaringType.FullName, "System.Diagnostics.StackFrameHelper")
                && c > 0)
            {
                //StackFrameHelper.InitializeSourceInfo calls Type.GetType and brings up TypeLoadException 
                //but it is not thrown, because the exception block swallows all exceptions 
                return true;
            }

            if (EqualsInvariant(m.Name, "GetWinRTContext") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Threading.SynchronizationContext")
                && c > 0)
            {
                //Tries to load System.Threading.WinRTSynchronizationContextFactory via GetType, 
                //and brings up TypeLoadException that usually does no happen
                return true;
            }

            if (EqualsInvariant(m.Name, "CreatePermission") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.Security.Util.XMLUtil")
                && c > 0)
            {
                //Tries to load security permission type using GetType, and brings up TypeLoadException. 
                //But the exception can only happen when we have corrupted permission set. Since it 
                //is rare, this method is excluded
                return true;
            }

            if (EqualsInvariant(m.Name, "get_Now") &&
                EqualsInvariant(m.DeclaringType.FullName, "System.DateTime")
                && c > 0)
            {
                //Can throw InvalidTimeZoneException, but usually args passed to  
                //CreateCustomTimeZone are correct and exception does not happen 
                return true;
            }

            return false;
        }

        static void GetExceptionsRecursive(
            MethodBase m,GetExceptionsRecursive_Context ctx,int c,bool returned
            )
        {
            if (!returned && ctx.visited.Contains(m)) return;
            ctx.visited.Add(m);

            //exclude some common false positives
            if (IsExcluded(m, c)) return;

            string mstr = "";
            mstr = m.DeclaringType.FullName + "." + CilVisualization.MethodToString(m);
            
            ctx.callstack.Push(mstr);
            string stackstr = String.Join(Environment.NewLine, ctx.callstack);

            if (c > 100000)
            {
                throw new ApplicationException("Error: recursion is too deep in GetExceptions");
            }

            try
            {
                CilGraph graph;

                try
                {
                    graph = CilGraph.Create(m);
                }
                catch (Exception ex) when ((ex is TypeLoadException || ex is CilParserException)&&c>0)
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
                            //the exception instance was constructed directly on the stack
                            MethodBase constr = prev.Instruction.ReferencedMember as MethodBase;
                            if (constr != null)
                            {
                                bool added = AddTypeToResults(constr.DeclaringType, stackstr, m, ctx, c);

                                if (added)
                                {
                                    System.Diagnostics.Debug.WriteLine("Newobj: " + constr.DeclaringType.ToString());
                                }
                            }
                        }
                        else if (prev.Instruction.OpCode == OpCodes.Ldloc)
                        {
                            //the exception instance was stored in local
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
                                bool added = AddTypeToResults(lt, stackstr, m, ctx, c);

                                if (added)
                                {
                                    System.Diagnostics.Debug.WriteLine("Local: " + lt.ToString());
                                }
                            }
                        }
                        else if (prev.Instruction.OpCode.FlowControl == FlowControl.Call)
                        {
                            //the exception instance is a return value of the method
                            MethodBase target = prev.Instruction.ReferencedMember as MethodBase;

                            if (target != null) GetExceptionsRecursive(target, ctx, c + 1, true);
                        }
                    }
                    else if (returned && node.Instruction.OpCode == OpCodes.Ret)
                    {
                        //the method's return value is an exception that will be thrown

                        CilGraphNode prev = node.Previous;
                        if (prev == null) continue;

                        if (prev.Instruction.OpCode == OpCodes.Newobj)
                        {
                            MethodBase constr = prev.Instruction.ReferencedMember as MethodBase;
                            if (constr != null)
                            {
                                bool added = AddTypeToResults(constr.DeclaringType, stackstr, m, ctx, c);

                                if (added)
                                {
                                    System.Diagnostics.Debug.WriteLine("Method: " + constr.DeclaringType.ToString());
                                }
                            }
                        }
                    }
                    else if (node.Instruction.OpCode.FlowControl == FlowControl.Call)
                    {
                        MethodBase target = node.Instruction.ReferencedMember as MethodBase;

                        //if it is a virtual call to virtual method, the real called method 
                        //is determined at runtime, so we skip analysis in this case 

                        if (target != null && 
                            !(node.Instruction.OpCode==OpCodes.Callvirt && target.IsVirtual)
                            )
                        {
                            //recursively check exceptions of the called method
                            GetExceptionsRecursive(target, ctx, c + 1,false);
                        }
                    }//endif
                }//end foreach
            }//end try
            finally
            {
                ctx.callstack.Pop();
            }
        }

        public static IEnumerable<ExceptionInfo> GetExceptions(MethodBase m)
        {
            Dictionary<Type,ExceptionInfo> results = new Dictionary<Type, ExceptionInfo>();
            HashSet<MethodBase> visited = new HashSet<MethodBase>(new MethodComparer());
            GetExceptionsRecursive_Context ctx = new GetExceptionsRecursive_Context();
            ctx.results = results;
            ctx.visited = visited;
            ctx.first = m;
            GetExceptionsRecursive(m, ctx,0,false);

            foreach (ExceptionInfo ex in results.Values) yield return ex;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(1000);
            if (type != null) sb.AppendLine(type.FullName);
            sb.AppendLine();
            sb.AppendLine("--- Callstack: ---");
            sb.AppendLine(stack);
            sb.AppendLine("------------------");
            return sb.ToString();
        }
    }
}
