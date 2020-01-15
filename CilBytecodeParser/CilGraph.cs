/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilBytecodeParser
{
    /// <summary>
    /// Represents a CIL graph, a graph that reflects a flow of control between CIL instructions in the method
    /// </summary>
    /// <remarks>
    /// CIL graph is a directed graph with nodes representing CIL instructions withing method body and edges representing how control flows between them when runtime executes method. The root of the graph is the first instruction of the method. Each node stores a reference to the next instruction (which is usually executed after it) and, if it's a jump instruction, a reference to the branch target (an instruction that would be executed if the condition for the jump is met). For convenience, each instruction serving as branch target is assigned a label, a string that identify it. The last instruction of the method has null as its next instruction reference.
    /// 
    /// Use <see cref="CilAnalysis.GetGraph"/> method to create CIL graph for a method.
    /// </remarks>
    public class CilGraph
    {        
        static IList<ExceptionHandlingClause> FindTryBlocks(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");
            IList<ExceptionHandlingClause> res = new List<ExceptionHandlingClause>(list.Count);

            foreach (var block in list)
            {
                if (block.TryOffset >= start && block.TryOffset < end) res.Add(block);
            }

            return res;
        }

        private class TryBlockComparer : IEqualityComparer<ExceptionHandlingClause>
        {
            public bool Equals(ExceptionHandlingClause x, ExceptionHandlingClause y)
            {
                if (x == null)
                {
                    if (y == null) return true;
                    else return false;
                }
                else
                {
                    if (y != null)
                    {
                        return x.TryOffset == y.TryOffset && x.TryLength == y.TryLength;
                    }
                    else return false;
                }
            }

            public int GetHashCode(ExceptionHandlingClause obj)
            {
                if (obj == null) return 0;
                else return obj.TryOffset + obj.TryLength;
            }
        }

        static HashSet<ExceptionHandlingClause> FindDistinctTryBlocks(IList<ExceptionHandlingClause> list)
        {
            if (list == null) throw new ArgumentNullException("list");
            HashSet<ExceptionHandlingClause> set = new HashSet<ExceptionHandlingClause>(new TryBlockComparer());           

            foreach (var block in list)
            {
                set.Add(block);
            }

            return set;
        }

        static IList<ExceptionHandlingClause> FindFilterBlocks(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");
            IList<ExceptionHandlingClause> res = new List<ExceptionHandlingClause>(list.Count);

            foreach (var block in list)
            {
                if (block.Flags != ExceptionHandlingClauseOptions.Filter) continue;
                if (block.FilterOffset >= start && block.FilterOffset < end) res.Add(block);
            }

            return res;
        }

        static IList<ExceptionHandlingClause> FindHandlerBlocks(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");

            IList<ExceptionHandlingClause> res = new List<ExceptionHandlingClause>(list.Count);
            foreach (var block in list)
            {
                if (block.HandlerOffset >= start && block.HandlerOffset < end) res.Add(block);
            }
            return res;
        }

        static IList<ExceptionHandlingClause> FindBlockEnds(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");

            IList<ExceptionHandlingClause> res = new List<ExceptionHandlingClause>(list.Count);
            foreach (var block in list)
            {
                if (block.HandlerOffset + block.HandlerLength >= start && block.HandlerOffset + block.HandlerLength < end) res.Add(block);
            }
            return res;
        }        

        /// <summary>
        /// Raised when error occurs in one of the methods in this class
        /// </summary>
        public static event EventHandler<CilErrorEventArgs> Error;

        /// <summary>
        /// Raises Error event
        /// </summary>
        /// <param name="sender">object that generated event</param>
        /// <param name="e">event arguments</param>
        protected static void OnError(object sender, CilErrorEventArgs e)
        {
            EventHandler<CilErrorEventArgs> handler = Error;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// A root node of this graph (the first instruction in the method)
        /// </summary>
        protected CilGraphNode _Root;

        /// <summary>
        /// A method object for which this graph is built
        /// </summary>
        protected MethodBase _Method;

        /// <summary>
        /// Creates new CIL graph. (Insfrastructure; not intended for user code)
        /// </summary>
        /// <param name="root">Root node</param>
        /// <param name="mb">Method associated with this graph object</param>
        /// <remarks>Use <see cref="CilAnalysis.GetGraph"/> method to create CIL graph for a method instead of using this contructor.</remarks>
        public CilGraph(CilGraphNode root, MethodBase mb)
        {
            this._Root = root;
            this._Method = mb;
        }

        /// <summary>
        /// Gets a root node of this graph (the first instruction in the method)
        /// </summary>
        public CilGraphNode Root { get { return this._Root; } }

        /// <summary>
        /// Gets a method for which this graph is built
        /// </summary>
        public MethodBase Method { get { return this._Method; } }  

        /// <summary>
        /// Writes the CIL code corresponding to this graph into the specified TextWriter, optionally including signature, 
        /// default parameter values, attributes and method header
        /// </summary>
        /// <param name="output">The destination TextWriter, or null to use standard output</param>
        /// <param name="IncludeSignature">Indicates that method signature should be included in the output</param>
        /// <param name="IncludeDefaults">Indicates that default parameter values should be included in the output</param>
        /// <param name="IncludeAttributes">Indicates that custom attributes should be included in the output</param>
        /// <param name="IncludeHeader">Indicates that method header should be included in the output</param>
        /// <remarks>
        /// <para>The CIL code produced by this API is intended mainly for reading, not compiling. It is not guaranteed to be a valid  input for CIL assembler.</para>
        /// <para>Method header contains information such as maximum stack size and local variable types.</para>
        /// </remarks>
        public void Print(
            TextWriter output = null, 
            bool IncludeSignature = false, 
            bool IncludeDefaults = false, 
            bool IncludeAttributes = false,
            bool IncludeHeader = false)
        {
            if (output == null) output = Console.Out;

            CilGraphNode node = this._Root;
                        
            int n_iter = 0;
            IList<ExceptionHandlingClause> trys = new List<ExceptionHandlingClause>();
            bool block_end = false;
            MethodBody body = null;
            ParameterInfo[] pars = this._Method.GetParameters();

            try
            {
                body = this._Method.GetMethodBody();
                if (body != null) trys = body.ExceptionHandlingClauses;
            }
            catch (Exception ex)
            {
                string error = "Exception occured when trying to get method body.";
                OnError(this, new CilErrorEventArgs(ex, error));
            }

            if (IncludeSignature)
            {
                output.Write(".method "); //signature

                if (this._Method.IsPublic) output.Write("public ");
                else if (this._Method.IsPrivate) output.Write("private ");
                else if (this._Method.IsAssembly) output.Write("assembly "); //internal
                else if (this._Method.IsFamily) output.Write("family "); //protected
                else output.Write("famorassem "); //protected internal

                if (this._Method.IsHideBySig) output.Write("hidebysig ");

                if (this._Method.IsAbstract) output.Write("abstract ");

                if (this._Method.IsVirtual) output.Write("virtual ");

                if (this._Method.IsStatic) output.Write("static ");
                else output.Write("instance ");

                if (this._Method.CallingConvention == CallingConventions.VarArgs)
                {
                    output.Write("vararg ");
                }

                MethodInfo mi = this._Method as MethodInfo;
                string rt = "";
                if (mi != null) rt = CilAnalysis.GetTypeName(mi.ReturnType) + " ";
                output.Write(rt);

                output.Write(this._Method.Name);

                if (this._Method.IsGenericMethod)
                {
                    output.Write('<');

                    Type[] args = this._Method.GetGenericArguments();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i >= 1) output.Write(", ");

                        if (args[i].IsGenericParameter) output.Write(args[i].Name);
                        else output.Write(CilAnalysis.GetTypeName(args[i]));
                    }

                    output.Write('>');
                }

                output.Write('(');

                for (int i = 0; i < pars.Length; i++)
                {
                    if (i >= 1) output.WriteLine(", ");
                    else output.WriteLine();

                    output.Write("    ");
                    if (pars[i].IsOptional) output.Write("[opt] ");

                    output.Write(CilAnalysis.GetTypeName(pars[i].ParameterType));

                    string parname;
                    if (pars[i].Name != null) parname = pars[i].Name;
                    else parname = "par" + (i + 1).ToString();

                    output.Write(' ');
                    output.Write(parname);

                }

                if (pars.Length > 0) output.WriteLine();
                output.Write(')');
                output.WriteLine(" cil managed {");
            }

            if (IncludeDefaults)
            {
                //optional parameters
                for (int i = 0; i < pars.Length; i++)
                {
                    if (pars[i].IsOptional && pars[i].RawDefaultValue != DBNull.Value)
                    {
                        output.Write(" .param [");
                        output.Write((i + 1).ToString());
                        output.Write("] = ");

                        if (pars[i].RawDefaultValue != null)
                        {
                            if (pars[i].RawDefaultValue.GetType() == typeof(string))
                            {
                                output.Write('"');
                                output.Write(CilAnalysis.EscapeString(pars[i].RawDefaultValue.ToString()));
                                output.Write('"');
                            }
                            else //most of the types...
                            {
                                output.Write(CilAnalysis.GetTypeName(pars[i].ParameterType));
                                output.Write('(');
                                output.Write(Convert.ToString(pars[i].RawDefaultValue, System.Globalization.CultureInfo.InvariantCulture));
                                output.Write(')');
                            }
                        }
                        else output.Write("nullref");

                        output.WriteLine();
                    }
                }
            }

            if (IncludeAttributes)
            {
                //attributes
                try
                {
                    object[] attrs = this._Method.GetCustomAttributes(false);
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        Type t = attrs[i].GetType();
                        ConstructorInfo[] constr = t.GetConstructors();
                        string s_attr;

                        if (constr.Length == 1)
                        {
                            s_attr = CilAnalysis.MethodToString(constr[0]);
                            int parcount = constr[0].GetParameters().Length;      

                            if (parcount == 0 && t.GetFields(BindingFlags.Public & BindingFlags.Instance).Length == 0 &&
                                t.GetProperties(BindingFlags.Public | BindingFlags.Instance).
                                Where((x) => x.DeclaringType != typeof(Attribute) && x.CanWrite == true).Count() == 0
                                )
                            {
                                output.Write(" .custom ");
                                output.Write(s_attr);
                                output.WriteLine(" = ( 01 00 00 00 )"); //Atribute prolog & zero number of arguments (ECMA-335 II.23.3 Custom attributes)
                            }
                            else
                            {
                                output.Write(" //.custom ");
                                output.Write(s_attr);
                                output.WriteLine();
                            }                            
                        }
                        else
                        {
                            output.Write(" //.custom ");
                            s_attr = CilAnalysis.GetTypeNameInternal(t);
                            output.WriteLine(s_attr);
                        }
                    }
                }
                catch (InvalidOperationException) { }
                catch (TypeLoadException ex)
                {
                    OnError(this, new CilErrorEventArgs(ex, "Failed to load attributes for " + this._Method.ToString()));
                }
            }

            //method header           
            if (body != null && IncludeHeader)
            {
                output.WriteLine(" .maxstack " + body.MaxStackSize.ToString());

                //local variables
                if (body.LocalVariables != null && body.LocalVariables.Count > 0)
                {
                    output.Write(" .locals");

                    if (body.InitLocals) output.Write(" init ");

                    output.Write('(');
                    for (int i = 0; i < body.LocalVariables.Count; i++)
                    {
                        if (i >= 1) output.Write(",\r\n   ");
                        var local = body.LocalVariables[i];
                        output.Write(CilAnalysis.GetTypeName(local.LocalType));
                        output.Write(" V_" + local.LocalIndex.ToString());
                    }
                    output.Write(')');
                    output.WriteLine();
                }
            }

            //instructions
            if (node != null)
            {
                output.WriteLine();
                                
                while (true)
                {
                    block_end = false;
                    CilInstruction instr = node.Instruction;

                    //exception handling clauses
                    IList<ExceptionHandlingClause> started_blocks = FindTryBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);
                    IList<ExceptionHandlingClause> filters = FindFilterBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);
                    IList<ExceptionHandlingClause> ended_blocks = FindBlockEnds(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);
                    HashSet<ExceptionHandlingClause> distinct_trys = FindDistinctTryBlocks(started_blocks);

                    for (int i = 0; i < distinct_trys.Count; i++)
                    {
                        output.WriteLine(" .try     {");
                    }

                    for (int i = 0; i < filters.Count; i++)
                    {
                        output.WriteLine(" }");
                        output.WriteLine(" filter {");
                    }

                    for (int i = 0; i < ended_blocks.Count; i++)
                    {
                        output.WriteLine(" }");
                        block_end = true;
                    }

                    var blocks = FindHandlerBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);

                    foreach (var block in blocks)
                    {
                        if (!block_end) output.WriteLine(" }");

                        if (block.Flags == ExceptionHandlingClauseOptions.Clause)
                        {
                            string st = "";
                            Type t = block.CatchType;
                            if (t != null) st = CilAnalysis.GetTypeNameInternal(t);
                            output.WriteLine(" catch " + st + " {");
                        }
                        else if (block.Flags == ExceptionHandlingClauseOptions.Finally)
                        {
                            output.WriteLine(" finally  {");
                        }
                        else if (block.Flags == ExceptionHandlingClauseOptions.Fault)
                        {
                            output.WriteLine(" .fault  {");
                        }
                    }

                    //if instruction is referenced as branch target, prepend label to it
                    if (!String.IsNullOrEmpty(node.Name)) output.Write(node.Name + ": ");
                    else output.Write("".PadLeft(10, ' '));

                    //if instruction itself targets branch, append its label
                    if (node.BranchTarget != null) output.Write(instr.Name.PadRight(9) + " " + node.BranchTarget.Name);
                    else output.Write(instr.ToString());

                    output.WriteLine();

                    if (node.Next == null) break; //last instruction
                    else node = node.Next;

                    n_iter++;
                    if (n_iter > 100000)
                    {
                        output.WriteLine(
                            "Error: Too many iterations while trying to process graph (possibly a cyclic or extremely large graph)"
                            );
                        break;
                    }
                }// end while
            }//endif

            if (IncludeSignature) output.WriteLine("}");            
        }
                
        /// <summary>
        /// Returns CIL code corresponding to this graph as a string
        /// </summary>
        /// <remarks>The CIL code returned by this API is intended mainly for reading, not compiling. It is not guaranteed to be a valid input for CIL assembler.</remarks>
        /// <returns>A string of CIL code</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(2048);
            StringWriter wr = new StringWriter(sb);

            using (wr)
            {
                this.Print(wr, true, true, true, true);
                return sb.ToString();
            }            
        }

        /// <summary>
        /// Enumerates nodes in this graph
        /// </summary>
        /// <returns>The collection of graph nodes</returns>
        public IEnumerable<CilGraphNode> GetNodes()
        {
            CilGraphNode node = this._Root;            

            while (true)
            {
                if (node == null) break;

                yield return node;

                if (node.Next == null) break; //last instruction
                else node = node.Next;
            }
        }

        /// <summary>
        /// Enumerates all instructions represented by this graph's nodes
        /// </summary>
        /// <returns>The collection of instructions</returns>
        public IEnumerable<CilInstruction> GetInstructions()
        {
            CilGraphNode node = this._Root;

            while (true)
            {
                if (node == null) break;

                yield return node.Instruction;

                if (node.Next == null) break; //last instruction
                else node = node.Next;
            }
        }

#if !NETSTANDARD
        /// <summary>
        /// Emits the entire content of this CIL graph into the specified IL generator, 
        /// optionally calling user callback for each processed instruction.
        /// </summary>
        /// <param name="gen">Target IL generator. </param>
        /// <param name="callback">User callback to be called for each processed instruction.</param>
        /// <remarks>Passing user callback into this method enables you to filter instructions that you want to be emitted 
        /// into target IL generator. 
        /// Return <see langword="true"/> to skip emitting instruction, or <see langword="false"/> to emit instruction.</remarks>
        public void EmitTo(ILGenerator gen, Func<CilInstruction,bool> callback = null)
        {
            Dictionary<uint,Label> labels=new Dictionary<uint,Label>();
            Label label;
            IList<ExceptionHandlingClause> trys = new List<ExceptionHandlingClause>();                

            MethodBody body = null;            
            body = this._Method.GetMethodBody();

            if (body == null)
            {
                throw new CilParserException("Cannot get method body. Method: " + this._Method.Name);
            }

            trys = body.ExceptionHandlingClauses;            

            //local variables
            if (body.LocalVariables != null)
            {                
                for (int i = 0; i < body.LocalVariables.Count; i++)
                {      
                    var local = body.LocalVariables[i];
                    gen.DeclareLocal(local.LocalType);                    
                }                
            }

            List<CilGraphNode> nodes = this.GetNodes().ToList();

            //first stage - create labels
            foreach (CilGraphNode node in nodes)
            {
                if (!String.IsNullOrEmpty(node.Name))
                {
                    //if instruction is marked with label, save label in dictionary
                    label = gen.DefineLabel();
                    labels[node.Instruction.OrdinalNumber] = label;                    
                }
            }

            //second stage - emit actual IL
            foreach (CilGraphNode node in nodes)
            {
                CilInstruction instr = node.Instruction;
                                
                //exception handling clauses
                IList<ExceptionHandlingClause> block_starts = FindTryBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);
                IList<ExceptionHandlingClause> filters = FindFilterBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);
                IList<ExceptionHandlingClause> block_ends = FindBlockEnds(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);                

                for (int i = 0; i < block_starts.Count; i++)
                {
                    gen.BeginExceptionBlock();                    
                }

                for (int i = 0; i < filters.Count; i++)
                {
                    gen.BeginExceptFilterBlock();
                }

                for (int i = 0; i < block_ends.Count; i++)
                {
                    gen.EndExceptionBlock();
                }                

                var blocks = FindHandlerBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);

                foreach (var block in blocks)
                {
                    if (block.Flags == ExceptionHandlingClauseOptions.Clause)
                    {
                        Type t = block.CatchType;
                        gen.BeginCatchBlock(t);
                    }
                    else if (block.Flags == ExceptionHandlingClauseOptions.Finally)
                    {                        
                        gen.BeginFinallyBlock();
                    }
                    else if (block.Flags == ExceptionHandlingClauseOptions.Fault)
                    {
                        gen.BeginFaultBlock();
                    }
                }

                //labels
                if (!String.IsNullOrEmpty(node.Name))
                {
                    //if instruction has label, mark it
                    if (labels.ContainsKey(instr.OrdinalNumber))
                    {
                        label = labels[instr.OrdinalNumber];
                        gen.MarkLabel(label);                        
                    }
                }
                                
                //user callback
                bool should_skip = false;

                if (callback != null)
                {
                    should_skip = callback(instr); //execute user callback                    
                }

                //instruction itself
                if (!should_skip)
                {
                    //if instruction is not processed by callback, emit it

                    if (node.BranchTarget != null)
                    {
                        //if this instruction references branch, find label and emit jump instruction
                        if (labels.ContainsKey(node.BranchTarget.Instruction.OrdinalNumber))
                        {
                            label = labels[node.BranchTarget.Instruction.OrdinalNumber];

                            if (instr.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                            {
                                //convert short form instructions into long form, to prevent failure when
                                //method body is larger then expected

                                OpCode opc;
                                if (instr.OpCode == OpCodes.Brfalse_S) opc = OpCodes.Brfalse;
                                else if (instr.OpCode == OpCodes.Brtrue_S) opc = OpCodes.Brtrue;
                                else if (instr.OpCode == OpCodes.Leave_S) opc = OpCodes.Leave;
                                else if (instr.OpCode == OpCodes.Br_S) opc = OpCodes.Br;
                                else if (instr.OpCode == OpCodes.Blt_S) opc = OpCodes.Blt;
                                else if (instr.OpCode == OpCodes.Blt_Un_S) opc = OpCodes.Blt_Un;
                                else if (instr.OpCode == OpCodes.Bne_Un_S) opc = OpCodes.Bne_Un;
                                else if (instr.OpCode == OpCodes.Ble_S) opc = OpCodes.Ble;
                                else if (instr.OpCode == OpCodes.Ble_Un_S) opc = OpCodes.Ble_Un;
                                else throw new NotSupportedException("OpCode not supported: " + instr.OpCode.ToString());

                                gen.Emit(opc, label);
                            }
                            else //long form branching instruction
                            {
                                gen.Emit(instr.OpCode, label); //emit as-is
                            }
                        }
                        else throw new CilParserException("Cannot find label for branch instruction");
                    }
                    else
                    {
                        //emit regular instruction
                        instr.EmitTo(gen);
                    }
                }     

            }//end foreach            
        }
#endif

    }
}
