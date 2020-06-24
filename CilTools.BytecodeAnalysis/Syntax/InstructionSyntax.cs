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
    /// <summary>
    /// Represents instruction syntax in CIL assembler
    /// </summary>
    /// <remarks>The instruction syntax consists of the optional label, operation name, and the optional operand</remarks>
    public class InstructionSyntax:SyntaxNode
    {        
        CilGraphNode _node;
        SyntaxNode[] _labelnodes;
        SyntaxNode[] _operationnodes;
        SyntaxNode[] _operandnodes;

        void CreateNodes(string lead, CilGraphNode graphnode)
        {
            List<SyntaxNode> labelnodes = new List<SyntaxNode>();
            string pad,pad2;

            //label and operation nodes
            if (!String.IsNullOrEmpty(graphnode.Name))
            {
                labelnodes.Add(new IdentifierSyntax(lead + " ", graphnode.Name, String.Empty, false) { _parent = this });
                labelnodes.Add(new PunctuationSyntax(String.Empty, ":", " ") { _parent = this });
                pad = "";
            }
            else pad = lead+"".PadLeft(10, ' ');

            string name = graphnode.Instruction.OpCode.Name;
            int len = 12 - name.Length;

            if (len < 1) len = 1;

            pad2 = "".PadLeft(len);

            List<SyntaxNode> operationnodes = new List<SyntaxNode>();
            operationnodes.Add(new KeywordSyntax(pad, name, pad2, KeywordKind.InstructionName) { _parent = this });

            //operand nodes
            List<SyntaxNode> operandnodes = new List<SyntaxNode>();
            
            if (graphnode.BranchTarget != null) //if instruction itself targets branch, append its label
            {
                operandnodes.Add( 
                    new IdentifierSyntax(String.Empty, this._node.BranchTarget.Name, Environment.NewLine, false) { 
                        _parent = this 
                    });
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

            for (int i = 0; i < operandnodes.Count; i++) operandnodes[i]._parent = this;
            
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

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            
            //if instruction is referenced as branch target, prepend label to it
            this.WriteLabel(target);

            this.WriteOperation(target);
            this.WriteOperand(target);
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            for (int i = 0; i < this._labelnodes.Length; i++) yield return this._labelnodes[i];

            for (int i = 0; i < this._operationnodes.Length; i++) yield return this._operationnodes[i];

            for (int i = 0; i < this._operandnodes.Length; i++) yield return this._operandnodes[i];
        }

        /// <summary>
        /// Gets the label name of this instruction
        /// </summary>
        public string Label
        {
            get
            {
                return _node.Name;
            }
        }

        /// <summary>
        /// Writes the label name of this instruction into the specified TextWriter
        /// </summary>
        public void WriteLabel(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            //if instruction is referenced as branch target, prepend label to it
            for (int i = 0; i < this._labelnodes.Length; i++) this._labelnodes[i].ToText(target);
            target.Flush();
        }

        /// <summary>
        /// Gets the operation name of this instruction
        /// </summary>
        public string Operation
        {
            get
            {
                return _node.Instruction.OpCode.Name;
            }
        }

        /// <summary>
        /// Writes the operation name of this instruction into the specified TextWriter
        /// </summary>
        public void WriteOperation(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            for (int i = 0; i < this._operationnodes.Length; i++) this._operationnodes[i].ToText(target);
            target.Flush();
        }

        /// <summary>
        /// Returns the instruction instance corresponding to this node
        /// </summary>
        public CilInstruction Instruction { get { return _node.Instruction; } }

        /// <summary>
        /// Writes the operand of this instruction into the specified TextWriter
        /// </summary>
        public void WriteOperand(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            for (int i = 0; i < this._operandnodes.Length; i++) this._operandnodes[i].ToText(target);
            target.Flush();
        }

        /// <summary>
        /// Gets the text representation of this instruction's operand 
        /// </summary>
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

        /// <summary>
        /// Gets the collection of nodes representing this instruction operand's syntax
        /// </summary>
        public IEnumerable<SyntaxNode> OperandSyntax
        {
            get
            {
                for (int i = 0; i < this._operandnodes.Length; i++) yield return this._operandnodes[i];
            }
        }
    }
}
