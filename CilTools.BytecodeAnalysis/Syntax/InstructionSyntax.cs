/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    public class InstructionSyntax:SyntaxElement
    {        
        CilGraphNode _node;

        internal InstructionSyntax(string lead,CilGraphNode node)
        {
            if (lead == null) lead = "";
            this._lead = lead;
            this._node = node;
        }

        public override void ToText(TextWriter target)
        {
            this.WriteLead(target);

            //if instruction is referenced as branch target, prepend label to it
            this.WriteLabel(target);

            /*if (this._node.BranchTarget != null) //if instruction itself targets branch, append its label
            {
                target.Write(this.Operation.PadRight(9) + " " + this._node.BranchTarget.Name);
            }
            else
            {
                CilGraphNode[] swtargets = this._node.GetSwitchTargets();

                if (swtargets.Length > 0) //append switch target list
                {
                    target.Write(this.Operation.PadRight(11));
                    target.Write('(');

                    for (int i = 0; i < swtargets.Length; i++)
                    {
                        if (i >= 1) target.Write(',');
                        target.Write(swtargets[i].Name);
                    }

                    target.Write(' ');
                    target.Write(')');
                }
                else target.Write(this.Instruction.ToString()); //print regular instruction
            }*/

            this.WriteOperation(target);
            this.WriteOperand(target);
        }

        public void WriteLead(TextWriter target)
        {
            target.Write(this._lead);
            target.Flush();
        }

        public string Label
        {
            get
            {
                return _node.Name;
            }
        }

        public void WriteLabel(TextWriter target)
        {
            //if instruction is referenced as branch target, prepend label to it
            if (!String.IsNullOrEmpty(this.Label)) target.Write(this.Label + ": ");
            else target.Write("".PadLeft(10, ' '));
            target.Flush();
        }

        public string Operation
        {
            get
            {
                return _node.Instruction.OpCode.Name;
            }
        }

        public void WriteOperation(TextWriter target)
        {
            if (this._node.BranchTarget != null) 
            {
                target.Write(this.Operation.PadRight(9));
            }
            else
            {
                target.Write(this.Operation.PadRight(11));
            }

            target.Flush();
        }

        public CilInstruction Instruction { get { return _node.Instruction; } }

        public void WriteOperand(TextWriter target)
        {
            if (this._node.BranchTarget != null) //if instruction itself targets branch, append its label
            {
                target.Write(this._node.BranchTarget.Name);
            }
            else
            {
                CilGraphNode[] swtargets = this._node.GetSwitchTargets();

                if (swtargets.Length > 0) //append switch target list
                {
                    target.Write('(');

                    for (int i = 0; i < swtargets.Length; i++)
                    {
                        if (i >= 1) target.Write(',');
                        target.Write(swtargets[i].Name);
                    }

                    target.Write(' ');
                    target.Write(')');
                }
                else
                {
                    this.Instruction.OperandToString(target); //print regular instruction operand
                }
            }

            target.Flush();
        }

        public string Operand
        {
            get
            {
                StringBuilder sb = new StringBuilder(200);
                StringWriter wr = new StringWriter(sb);
                this.WriteOperand(wr);                
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return _node.ToString();
        }
    }
}
