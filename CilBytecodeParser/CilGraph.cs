/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
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
        static bool FindTryBlock(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");

            foreach (var block in list)
            {
                if (block.TryOffset >= start && block.TryOffset < end) return true;
            }

            return false;
        }

        static uint GetTryBlocksCount(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");
            uint res = 0;

            foreach (var block in list)
            {
                if (block.TryOffset >= start && block.TryOffset < end) res++;
            }

            return res;
        }

        static IList<ExceptionHandlingClause> FindHandlerBlocks(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");

            IList<ExceptionHandlingClause> res = new List<ExceptionHandlingClause>();
            foreach (var block in list)
            {
                if (block.HandlerOffset >= start && block.HandlerOffset < end) res.Add(block);
            }
            return res;
        }

        static bool FindBlockEnd(IList<ExceptionHandlingClause> list, uint start, uint end)
        {
            if (list == null) throw new ArgumentNullException("list");

            foreach (var block in list)
            {
                if (block.HandlerOffset + block.HandlerLength >= start && block.HandlerOffset + block.HandlerLength < end)
                    return true;
            }
            return false;
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
        /// Returns CIL code corresponding to this graph as a string
        /// </summary>
        /// <remarks>The CIL code returned by this API is intended mainly for reading, not compiling. It is not guaranteed to be a valid input for CIL assembler.</remarks>
        /// <returns>A string of CIL code</returns>
        public override string ToString()
        {
            CilGraphNode node = this._Root;            

            StringBuilder sb = new StringBuilder();
            int n_iter = 0;
            IList<ExceptionHandlingClause> trys=new List<ExceptionHandlingClause>();
            bool block_end = false;
             MethodBody body=null;

            try
            {
                body = this._Method.GetMethodBody();
                if(body!=null) trys = body.ExceptionHandlingClauses;
            }
            catch (Exception ex)
            {
                string error = "Exception occured when trying to get method body.";
                OnError(this, new CilErrorEventArgs(ex, error));   
            }

            sb.Append(".method ");

            //signature
            if (this._Method.IsPublic) sb.Append("public ");
            else if (this._Method.IsPrivate) sb.Append("private ");
            else sb.Append("protected ");

            if (this._Method.IsHideBySig) sb.Append("hidebysig ");

            if (this._Method.IsStatic) sb.Append("static ");

            if (this._Method.CallingConvention == CallingConventions.VarArgs)
            {
                sb.Append("vararg ");
            }

            ParameterInfo[] pars = this._Method.GetParameters();
            MethodInfo mi = this._Method as MethodInfo;
            string rt = "";
            if (mi != null) rt = CilAnalysis.GetTypeName(mi.ReturnType)+" ";
            sb.Append(rt);

            sb.Append(this._Method.Name);
            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");
                sb.Append(CilAnalysis.GetTypeName(pars[i].ParameterType));                
            }

            sb.Append(')');
            sb.AppendLine(" cil managed {");
            
            if (body != null)
            {
                sb.AppendLine(" .maxstack "+body.MaxStackSize.ToString());

                //local variables
                if (body.LocalVariables != null && body.LocalVariables.Count > 0)
                {
                    sb.Append(" .locals");

                    if (body.InitLocals) sb.Append(" init ");

                    sb.Append('(');
                    for (int i = 0; i < body.LocalVariables.Count;i++ )
                    {
                        if (i >= 1) sb.Append(",\r\n   ");
                        var local = body.LocalVariables[i];
                        sb.Append(CilAnalysis.GetTypeName(local.LocalType));
                        sb.Append(" V_" + local.LocalIndex.ToString());
                    }
                    sb.Append(')');
                    sb.AppendLine();
                }
            }

            if (node != null)
            {
                sb.AppendLine();

                //instructions
                while (true)
                {
                    block_end = false;
                    CilInstruction instr = node.Instruction;

                    //exception handling clauses

                    if (FindTryBlock(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize))
                    {
                        sb.AppendLine(" .try     {");
                    }

                    if (FindBlockEnd(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize))
                    {
                        sb.AppendLine(" }");
                        block_end = true;
                    }

                    var blocks = FindHandlerBlocks(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);

                    foreach (var block in blocks)
                    {
                        if (!block_end) sb.AppendLine(" }");

                        if (block.Flags == ExceptionHandlingClauseOptions.Clause)
                        {
                            string st = "";
                            Type t = block.CatchType;
                            if (t != null) st = CilAnalysis.GetTypeNameInternal(t);
                            sb.AppendLine(" .catch " + st + " {");
                        }
                        else if (block.Flags == ExceptionHandlingClauseOptions.Finally)
                        {
                            sb.AppendLine(" finally  {");
                        }
                    }

                    //if instruction is referenced as branch target, prepend label to it
                    if (!String.IsNullOrEmpty(node.Name)) sb.Append(node.Name + ": ");
                    else sb.Append("".PadLeft(10, ' '));

                    //if instruction itself targets branch, append its label
                    if (node.BranchTarget != null) sb.Append(instr.Name.PadRight(9) +" "+ node.BranchTarget.Name);
                    else sb.Append(instr.ToString());

                    sb.AppendLine();

                    if (node.Next == null) break; //last instruction
                    else node = node.Next;

                    n_iter++;
                    if (n_iter > 100000)
                    {
                        sb.AppendLine(
                            "Error: Too many iterations while trying to process graph (possibly a cyclic or extremely large graph)"
                            );
                        break;
                    }
                }// end while
            }//endif

            sb.AppendLine("}");
            return sb.ToString();
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
                uint trys_count = GetTryBlocksCount(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize);
                for (int i = 0; i < trys_count; i++)
                {
                    gen.BeginExceptionBlock();                    
                }

                if (FindBlockEnd(trys, instr.ByteOffset, instr.ByteOffset + instr.TotalSize))
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

    }
}
