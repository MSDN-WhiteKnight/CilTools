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
    /// Represents a node node in the CIL graph. A node describes an instruction and its connection with other instructions
    /// </summary>
    public class CilGraphNode
    {
        /// <summary>
        /// CIL instruction associated with this node
        /// </summary>
        protected CilInstruction _Instruction;

        /// <summary>
        /// Optional label name associated with this node
        /// </summary>
        protected string _Name;

        /// <summary>
        /// A reference to the node that represents instruction directly preceding current instruction in the method bytecode
        /// </summary>
        protected CilGraphNode _Previous=null;

        /// <summary>
        /// A reference to the node that represents instruction directly following current instruction in the method bytecode
        /// </summary>
        protected CilGraphNode _Next = null;

        /// <summary>
        /// A reference to the node that represents instruction which is a target of the current instruction, if applicable
        /// </summary>
        protected CilGraphNode _BranchTarget = null;

        /// <summary>
        /// Gets CIL instruction associated with this node
        /// </summary>
        public CilInstruction Instruction { get { return this._Instruction; } }

        /// <summary>
        /// Gets label name associated with this node
        /// </summary>
        public string Name { get { return this._Name; } }

        /// <summary>
        /// Gets a reference to the node that represents instruction directly preceding current instruction in the method bytecode
        /// </summary>
        public CilGraphNode Previous { get { return this._Previous; } }

        /// <summary>
        /// Gets a reference to the node that represents instruction directly following current instruction in the method bytecode
        /// </summary>
        public CilGraphNode Next { get { return this._Next; } }

        /// <summary>
        /// Gets a reference to the node that represents instruction which is a target of the current instruction, if applicable
        /// </summary>
        public CilGraphNode BranchTarget { get { return this._BranchTarget; } }

        /// <summary>
        /// Creates new CilGraphNode object
        /// </summary>
        protected CilGraphNode()
        {
            this._Name = "";
            this._Instruction = new CilInstruction(OpCodes.Nop);
        }
        
        /// <summary>
        /// Returns text representation of this node as a line of CIL code
        /// </summary>
        /// <returns>String that contatins a text representation of this node</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if(!String.IsNullOrEmpty(this._Name)) sb.Append(this._Name + ": ");
            sb.Append(this._Instruction.ToString());
            return sb.ToString();
        }
    }

    /// <summary>
    /// A mutable version of CilGraphNode class
    /// </summary>    
    public class CilGraphNodeMutable : CilGraphNode
    {
        /// <summary>
        /// Creates new mutable CIL graph node object
        /// </summary>
        /// <param name="instr">An instruction associated with this node</param>
        public CilGraphNodeMutable(CilInstruction instr)
        {
            this._Name = "";
            this._Instruction = instr;
        }

        /// <summary>
        /// Gets or set CIL instruction associated with this node
        /// </summary>
        public new CilInstruction Instruction
        {
            get { return this._Instruction; }
            set { this._Instruction = value; }
        }

        /// <summary>
        /// Gets or sets label name associated with this node
        /// </summary>
        public new string Name
        {
            get { return this._Name; }
            set { this._Name = value; }
        }

        /// <summary>
        /// Gets or sets a reference to the node that represents instruction directly preceding current instruction in the method bytecode
        /// </summary>
        public new CilGraphNode Previous
        {
            get { return this._Previous; }
            set { this._Previous = value; }
        }

        /// <summary>
        /// Gets or sets a reference to the node that represents instruction directly following current instruction in the method bytecode
        /// </summary>
        public new CilGraphNode Next
        {
            get { return this._Next; }
            set { this._Next = value; }
        }

        /// <summary>
        /// Gets or sets a reference to the node that represents instruction which is a target of the current instruction, if applicable
        /// </summary>
        public new CilGraphNode BranchTarget
        {
            get { return this._BranchTarget; }
            set { this._BranchTarget = value; }
        }
    }
}
