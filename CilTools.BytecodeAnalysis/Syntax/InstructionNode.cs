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
    public class InstructionNode:SyntaxTreeNode
    {
        string _lead;
        CilGraphNode _node;

        internal InstructionNode(string lead,CilGraphNode node)
        {
            if (lead == null) lead = "";
            this._lead = lead;
            this._node = node;
        }

        public override IEnumerable<SyntaxTreeNode> Children
        {
            get { return new SyntaxTreeNode[] { }; }
        }

        public override void ToText(TextWriter target)
        {
            target.Write(_node.ToString());
        }

        public string Label
        {
            get
            {
                return _node.Instruction.Name;
            }
        }

        public string OpCode
        {
            get
            {
                return _node.Instruction.OpCode.Name;
            }
        }

        public CilInstruction Instruction { get { return _node.Instruction; } }

        public string Operand
        {
            get
            {
                StringBuilder sb = new StringBuilder(200);
                StringWriter wr = new StringWriter(sb);
                _node.Instruction.OperandToString(wr);
                wr.Flush();
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return _node.ToString();
        }
    }
}
