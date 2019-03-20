/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilBytecodeParser
{
    /// <summary>
    /// Provide static methods that assist in parsing and analysing CIL bytecode
    /// </summary>
    public static class CilAnalysis
    {
        /// <summary>
        /// Raised when error occurs in one of the methods in this class
        /// </summary>
        public static event EventHandler<CilErrorEventArgs> Error;

        static void OnError(object sender,CilErrorEventArgs e)
        {
            EventHandler<CilErrorEventArgs> handler = Error;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// Gets the name of .NET type in CIL notation
        /// </summary>
        /// <param name="t">Type for which name is requested</param>
        /// <returns>Short of full type name</returns>
        public static string GetTypeName(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");

            if (t.Equals(typeof(bool))) return "bool";
            else if (t.Equals(typeof(void))) return "void";
            else if (t.Equals(typeof(int))) return "int32";
            else if (t.Equals(typeof(uint))) return "uint32";
            else if (t.Equals(typeof(long))) return "int64";
            else if (t.Equals(typeof(ulong))) return "uint64";
            else if (t.Equals(typeof(short))) return "int16";
            else if (t.Equals(typeof(ushort))) return "uint16";
            else if (t.Equals(typeof(byte))) return "byte";
            else if (t.Equals(typeof(sbyte))) return "sbyte";
            else if (t.Equals(typeof(string))) return "string";
            else if (t.Equals(typeof(object))) return "object";
            else
            {                
                return GetTypeFullName(t);
            }
        }

        /// <summary>
        /// Gets the full name of .NET type in CIL notation
        /// </summary>
        /// <param name="t">Type for which name is requested</param>
        /// <returns>Full type name</returns>
        public static string GetTypeFullName(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");
            
            StringBuilder sb = new StringBuilder();

            if (t.IsValueType) sb.Append("valuetype ");
            else if (t.IsClass) sb.Append("class ");

            Assembly ass = t.Assembly;
            sb.Append('[');
            sb.Append(ass.GetName().Name);
            sb.Append(']');

            sb.Append(t.Namespace);
            sb.Append('.');
            sb.Append(t.Name);

            if (t.IsGenericType)
            {
                sb.Append('<');

                Type[] args=t.GetGenericArguments();
                for(int i=0;i<args.Length;i++)
                {
                    if (i >= 1) sb.Append(", ");
                    sb.Append(GetTypeName(args[i]));
                }

                sb.Append('>');
            }

            return sb.ToString();

        }

        /// <summary>
        /// Returns CIL graph that represents a specified method
        /// </summary>
        /// <param name="m">Method for which to build CIL graph</param>
        /// <returns>CIL graph object</returns>
        public static CilGraph GetGraph(MethodBase m)
        {
            if (m == null) throw new ArgumentNullException("m");

            List<CilInstruction> instructions;
            List<int> labels = new List<int>();

            try
            {
                instructions = CilReader.GetInstructions(m).ToList();
            }
            catch (Exception ex)
            {
                string error = "Exception occured when trying to read method's instructions.";
                OnError(m, new CilErrorEventArgs(ex, error));  
                return new CilGraph(null, m);
            }

            List<CilGraphNodeMutable> nodes = new List<CilGraphNodeMutable>(instructions.Count);
            CilGraphNode[] targets;
                   
            foreach (CilInstruction instr in instructions)
            {
                //if instruction references branch targets, add byte offset into labels
                if (instr.OpCode.OperandType == OperandType.ShortInlineBrTarget && instr.Operand != null)
                {
                    int target = (sbyte)instr.Operand + (int)instr.ByteOffset + (int)instr.TotalSize;

                    if (!labels.Contains(target)) labels.Add(target);
                }

                nodes.Add(new CilGraphNodeMutable(instr)); 
            }

            labels.Sort();

            targets = new CilGraphNodeMutable[labels.Count];

            //find all nodes that are referenced as labels and give them names

            foreach (CilGraphNodeMutable node in nodes)
            {
                CilInstruction instr = node.Instruction;

                for (int i = 0; i < labels.Count; i++)
                {
                    if (instr.ByteOffset == labels[i])
                    {
                        node.Name = " IL_" + (i + 1).ToString().PadLeft(4,'0');
                        targets[i] = node;
                        break;
                    }
                }                
            }

            //build the final graph

            for(int n=0;n<nodes.Count;n++)
            {
                CilInstruction instr = nodes[n].Instruction;                             

                //if instruction references branch target, connect it with respective node

                if (instr.OpCode.OperandType == OperandType.ShortInlineBrTarget && instr.Operand != null)
                {
                    int target = (sbyte)instr.Operand + (int)instr.ByteOffset + (int)instr.TotalSize;

                    for (int i = 0; i < labels.Count; i++)
                    {
                        if (target == labels[i])
                        {
                            nodes[n].BranchTarget = targets[i];
                            break;
                        }
                    }
                }

                //connect previous and next nodes
                if(n>0) nodes[n].Previous = nodes[n - 1];
                if (n < nodes.Count - 1) nodes[n].Next = nodes[n + 1];
               
            }

            return new CilGraph(nodes[0],m); //first node is a root node
        }

        /// <summary>
        /// Returns specified method CIL code as string
        /// </summary>
        /// <param name="m">Method for which to retreive CIL</param>
        /// <returns>CIL code string</returns>
        public static string MethodToText(MethodBase m)
        {
            if (m == null) return "(null)";

            return CilAnalysis.GetGraph(m).ToString();
        }

        /// <summary>
        /// Gets all methods that are referenced by the specified method
        /// </summary>
        /// <param name="mb">Method for which to retreive referenced methods</param>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(MethodBase mb)
        {
            if (mb == null) throw new ArgumentNullException("mb");

            List<MethodBase> results = new List<MethodBase>();

            var ins = CilReader.GetInstructions(mb);

            foreach (var instr in ins)
            {
                if (instr.ReferencedMember != null)
                {
                    if (instr.ReferencedMember is MethodBase)
                    {
                        var item = instr.ReferencedMember as MethodBase;
                        if (!results.Contains(item)) results.Add(item);                        
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets members (fields or methods) referenced by specified methods that match specified criteria
        /// </summary>
        /// <param name="mb">Method for which to retreive referenced members</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retreived</param>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(MethodBase mb, MemberCriteria flags)
        {
            if (mb == null) throw new ArgumentNullException("mb");

            List<MemberInfo> results = new List<MemberInfo>();

            //detect combinations of flags that can't produce any results
            if ((flags & MemberCriteria.Methods) == 0 && (flags & MemberCriteria.Fields) == 0) return results;
            if ((flags & MemberCriteria.Internal) == 0 && (flags & MemberCriteria.External) == 0) return results;
            
            Assembly ass;
            var ins = CilReader.GetInstructions(mb);

            foreach (var instr in ins)
            {
                if (instr.ReferencedMember == null) continue;

                if ((flags & MemberCriteria.Methods) != 0 && instr.ReferencedMember is MethodBase)
                {
                    var item = instr.ReferencedMember as MethodBase;
                    ass = item.DeclaringType.Assembly;

                    if (ass.FullName.ToLower().Trim() == mb.DeclaringType.Assembly.FullName.ToLower().Trim())
                    {
                        //internal
                        if ((flags & MemberCriteria.Internal) != 0 && !results.Contains(item)) results.Add(item);
                    }
                    else
                    {
                        //external
                        if ((flags & MemberCriteria.External) != 0 && !results.Contains(item)) results.Add(item);
                    }
                }
                else if ((flags & MemberCriteria.Fields) != 0 && instr.ReferencedMember is FieldInfo)
                {
                    var field = instr.ReferencedMember as FieldInfo;
                    ass = field.DeclaringType.Assembly;

                    if (ass.FullName.ToLower().Trim() == mb.DeclaringType.Assembly.FullName.ToLower().Trim())
                    {
                        //internal
                        if ((flags & MemberCriteria.Internal) != 0 && !results.Contains(field)) results.Add(field);
                    }
                    else
                    {
                        //external
                        if ((flags & MemberCriteria.External) != 0 && !results.Contains(field)) results.Add(field);
                    }
                }

            }

            return results;
        }



        /// <summary>
        /// Get all methods that are referenced by the code of the specified type
        /// </summary>
        /// <param name="t">Type for which to retreive referenced methods</param>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");

            //process regular methods
            var methods = t.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance                
                );
            List<MethodBase> results = new List<MethodBase>();

            foreach (var m in methods)
            {
                try
                {
                    var coll = GetReferencedMethods(m);                                       

                    foreach (var item in coll)
                    {
                        if (!results.Contains(item)) results.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to get a list of referenced methods.";
                    OnError(m, new CilErrorEventArgs(ex, error));                    
                }
            }

            //process constructors
            var constr = t.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );

            foreach (var m in constr)
            {
                try
                {
                    var coll = GetReferencedMethods(m);
                    
                    foreach (var item in coll)
                    {
                        if (!results.Contains(item)) results.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to get a list of referenced methods.";
                    OnError(m, new CilErrorEventArgs(ex, error));    
                }
            }

            return results;
        }

        /// <summary>
        /// Get all methods that are referenced by the code in the specified assembly
        /// </summary>
        /// <param name="ass">Assembly for which to retreive referenced methods</param>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(Assembly ass)
        {
            if (ass == null) throw new ArgumentNullException("ass");

            Type[] types = ass.GetTypes();
            List<MethodBase> results = new List<MethodBase>();

            foreach (Type t in types)
            {
                var coll = GetReferencedMethods(t);

                foreach (var item in coll)
                {
                    try
                    {
                        if (!results.Contains(item)) results.Add(item);
                    }
                    catch (Exception ex)
                    {
                        string error = "";
                        OnError(item, new CilErrorEventArgs(ex, error));   
                    }
                }
            }
            return results;
        }
    }

    [Flags]
    public enum MemberCriteria
    {
        External = 0x01,
        Internal = 0x02,
        Methods = 0x04,
        Fields = 0x08
    }
}
