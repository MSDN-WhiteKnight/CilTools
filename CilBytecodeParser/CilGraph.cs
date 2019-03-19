/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilBytecodeParser
{
    /// <summary>
    /// Represents a CIL graph, a graph that reflects a flow of control between CIL instructions in the method
    /// </summary>
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
                        if (i >= 1) sb.Append(", ");
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
                            if (t != null) st = t.ToString();
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
                    if (node.BranchTarget != null) sb.Append(instr.Name + " " + node.BranchTarget.Name);
                    else sb.Append(instr.ToString());

                    sb.AppendLine();

                    if (node.Next == null) break; //last instruction
                    else node = node.Next;

                    n_iter++;
                    if (n_iter > 100000)
                    {
                        throw new CilParserException(
                            "Too many iterations while trying to process graph (possibly a cyclic or extremely large graph)"
                            );
                    }
                }// end while
            }//endif

            sb.AppendLine("}");
            return sb.ToString();
        }

    }
}
