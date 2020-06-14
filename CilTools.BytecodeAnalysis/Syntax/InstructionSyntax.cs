/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    public class InstructionSyntax:SyntaxNode
    {        
        CilGraphNode _node;
        SyntaxNode[] _labelnodes;
        SyntaxNode[] _operationnodes;
        SyntaxNode[] _operandnodes;

        void CreateNodes(string lead, CilGraphNode graphnode)
        {
            List<SyntaxNode> labelnodes = new List<SyntaxNode>();
            string pad;

            //label and operation nodes
            if (!String.IsNullOrEmpty(graphnode.Name))
            {
                labelnodes.Add(new IdentifierSyntax(lead+" ", graphnode.Name, String.Empty, false));
                labelnodes.Add(new PunctuationSyntax(String.Empty, ":", " "));
                pad = "";
            }
            else pad = lead+"".PadLeft(10, ' ');

            List<SyntaxNode> operationnodes = new List<SyntaxNode>();
            operationnodes.Add(new GenericSyntax(pad + graphnode.Instruction.OpCode.Name.PadRight(11)));

            //operand nodes
            List<SyntaxNode> operandnodes = new List<SyntaxNode>();
            
            if (graphnode.BranchTarget != null) //if instruction itself targets branch, append its label
            {
                operandnodes.Add(new IdentifierSyntax(String.Empty, this._node.BranchTarget.Name, Environment.NewLine, false));
            }
            else
            {
                CilGraphNode[] swtargets = graphnode.GetSwitchTargets();

                if (swtargets.Length > 0) //append switch target list
                {
                    operandnodes.Add(new PunctuationSyntax(String.Empty, "(", String.Empty));

                    for (int i = 0; i < swtargets.Length; i++)
                    {
                        if (i >= 1) operandnodes.Add(new PunctuationSyntax(String.Empty, ",", String.Empty));
                        operandnodes.Add(new IdentifierSyntax(String.Empty, swtargets[i].Name, String.Empty, false));
                    }

                    operandnodes.Add(new PunctuationSyntax(String.Empty, ")", Environment.NewLine));
                }
                else
                {
                    //print regular instruction operand
                    IEnumerable<SyntaxNode> syntax = graphnode.Instruction.OperandToSyntax();

                    foreach (SyntaxNode item in syntax) operandnodes.Add(item);

                    operandnodes.Add(new GenericSyntax(Environment.NewLine));
                }
            }//endif

            if (operandnodes.Count == 0) operationnodes.Add(new GenericSyntax(Environment.NewLine));

            this._labelnodes = labelnodes.ToArray();
            this._operationnodes = operationnodes.ToArray();
            this._operandnodes = operandnodes.ToArray();
        }

        internal InstructionSyntax(string lead,CilGraphNode graphnode)
        {
            if (lead == null) lead = "";
            
            this._node = graphnode;
            this.CreateNodes(lead, graphnode);
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            
            //if instruction is referenced as branch target, prepend label to it
            this.WriteLabel(target);

            this.WriteOperation(target);
            this.WriteOperand(target);
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            for (int i = 0; i < this._labelnodes.Length; i++) yield return this._labelnodes[i];

            for (int i = 0; i < this._operationnodes.Length; i++) yield return this._operationnodes[i];

            for (int i = 0; i < this._operandnodes.Length; i++) yield return this._operandnodes[i];
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
            if (target == null) throw new ArgumentNullException("target");

            //if instruction is referenced as branch target, prepend label to it
            for (int i = 0; i < this._labelnodes.Length; i++) this._labelnodes[i].ToText(target);
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
            if (target == null) throw new ArgumentNullException("target");

            for (int i = 0; i < this._operationnodes.Length; i++) this._operationnodes[i].ToText(target);
            target.Flush();
        }

        public CilInstruction Instruction { get { return _node.Instruction; } }

        public void WriteOperand(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            for (int i = 0; i < this._operandnodes.Length; i++) this._operandnodes[i].ToText(target);
            target.Flush();
        }

        public string OperandString
        {
            get
            {
                StringBuilder sb = new StringBuilder(200);
                StringWriter wr = new StringWriter(sb);
                this.WriteOperand(wr);                
                return sb.ToString();
            }
        }

        public IEnumerable<SyntaxNode> OperandSyntax
        {
            get
            {
                for (int i = 0; i < this._operandnodes.Length; i++) yield return this._operandnodes[i];
            }
        }
    }
}
