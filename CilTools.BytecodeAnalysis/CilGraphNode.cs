/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents a node in the CIL graph. A node describes an instruction and its connection with other instructions
    /// </summary>
    /// <remarks>See <see cref="CilGraph"/> class documentation for more information about CIL graphs.</remarks>
    public class CilGraphNode
    {
        /// <summary>
        /// CIL instruction associated with this node
        /// </summary>
        internal CilInstruction _Instruction;

        /// <summary>
        /// Optional label name associated with this node
        /// </summary>
        internal string _Name;

        /// <summary>
        /// A reference to the node that represents instruction directly preceding current instruction in the method bytecode
        /// </summary>
        internal CilGraphNode _Previous = null;

        /// <summary>
        /// A reference to the node that represents instruction directly following current instruction in the method bytecode
        /// </summary>
        internal CilGraphNode _Next = null;

        /// <summary>
        /// A reference to the node that represents instruction which is a target of the current instruction, if applicable
        /// </summary>
        internal CilGraphNode _BranchTarget = null;

        /// <summary>
        /// An array of nodes that represents the jump table of the switch instruction, if applicable
        /// </summary>
        internal CilGraphNode[] _SwitchTargets = null;

        /// <summary>
        /// Gets CIL instruction associated with this node
        /// </summary>
        public CilInstruction Instruction { get { return this._Instruction; } }

        /// <summary>
        /// Gets label name associated with this node
        /// </summary>
        /// <remarks>Label names are assigned to instruction that serve as branch targets for convenience. If the instruction is not used as target, the value is empty string.</remarks>
        public string Name { get { return this._Name; } }

        /// <summary>
        /// Gets a reference to the node that represents instruction directly preceding current instruction in the method bytecode
        /// </summary>
        /// <remarks>For the first instruction in the method body, the value is null.</remarks>
        public CilGraphNode Previous { get { return this._Previous; } }

        /// <summary>
        /// Gets a reference to the node that represents instruction directly following current instruction in the method bytecode
        /// </summary>
        /// <remarks>Next instruction will be normally executed after current one, unless it is a jump instruction and the condition for jump is met. For the last instruction of the method body, the value is null.</remarks>
        public CilGraphNode Next { get { return this._Next; } }

        /// <summary>
        /// Gets a reference to the node that represents instruction which is a target of the current instruction, if applicable
        /// </summary>
        /// <remarks>Branch target is an instruction which would be called after current one if the condition for jump instruction is met. For non-jump instructions, the value is null.</remarks>
        public CilGraphNode BranchTarget { get { return this._BranchTarget; } }

        /// <summary>
        /// Gets an array of nodes that represents the jump table of the switch instruction, if applicable
        /// </summary>
        /// <remarks>Jump table is the sequence of instructions corresponding to the switch instruction. When runtime processes switch instruction, it will transfer control to one of the instructions based on the value pushed to the stack. For non-switch instructions, returns an empty array.</remarks>
        public CilGraphNode[] GetSwitchTargets()
        {            
            if (this._SwitchTargets == null) return new CilGraphNode[0];

            CilGraphNode[] res = new CilGraphNode[this._SwitchTargets.Length];
            Array.Copy(this._SwitchTargets, res, this._SwitchTargets.Length);
            return res;            
        }

        /// <summary>
        /// Creates new CilGraphNode object
        /// </summary>
        protected CilGraphNode()
        {
            this._Name = "";
            this._Instruction = CilInstruction.CreateEmptyInstruction(null);
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
    /// <remarks>Graph nodes are exposed as immutable <see cref="CilGraphNode"/> objects by API of this library, such as <see cref="CilAnalysis.GetGraph"/> method, because usually you don't need to alter their properties. However, these nodes are created as mutable objects and their actual runtime type is CilGraphNodeMutable; you can cast them to that type if you need to set their properties.</remarks>
    public class CilGraphNodeMutable : CilGraphNode
    {
        /// <summary>
        /// Creates new mutable CIL graph node object
        /// </summary>
        /// <param name="instr">An instruction associated with this node</param>
        /// <exception cref="System.ArgumentNullException">instr argument is null</exception>
        public CilGraphNodeMutable(CilInstruction instr)
        {
            if (instr == null) throw new ArgumentNullException("instr", "instr argument cannot be null");

            this._Name = "";
            this._Instruction = instr;
        }

        /// <summary>
        /// Gets or sets CIL instruction associated with this node
        /// </summary>
        public new CilInstruction Instruction
        {
            get { return this._Instruction; }
            set { this._Instruction = value; }
        }

        /// <summary>
        /// Gets or sets label name associated with this node
        /// </summary>
        /// <remarks>Label names are assigned to instruction that serve as branch targets for convenience. If the instruction is not used as target, the value is empty string.</remarks>
        public new string Name
        {
            get { return this._Name; }
            set { this._Name = value; }
        }

        /// <summary>
        /// Gets or sets a reference to the node that represents instruction directly preceding current instruction in the method bytecode
        /// </summary>
        /// <remarks>For the first instruction in the method body, the value is null.</remarks>
        public new CilGraphNode Previous
        {
            get { return this._Previous; }
            set { this._Previous = value; }
        }

        /// <summary>
        /// Gets or sets a reference to the node that represents instruction directly following current instruction in the method bytecode
        /// </summary>
        /// <remarks>Next instruction will be normally executed after current one, unless it is a jump instruction and the condition for jump is met. For the last instruction of the method body, the value is null.</remarks>
        public new CilGraphNode Next
        {
            get { return this._Next; }
            set { this._Next = value; }
        }

        /// <summary>
        /// Gets or sets a reference to the node that represents instruction which is a target of the current instruction, if applicable
        /// </summary>
        /// <remarks>Branch target is an instruction which would be called after current one if the condition for jump instruction is met. For non-jump instructions, the value is null.</remarks>
        public new CilGraphNode BranchTarget
        {
            get { return this._BranchTarget; }
            set { this._BranchTarget = value; }
        }

        /// <summary>
        /// Sets the array of nodes that represents the jump table of the switch instruction
        /// </summary>
        /// <remarks>Jump table is the sequence of instructions corresponding to the switch instruction. When runtime processes switch instruction, it will transfer control to one of the instructions based on the value pushed to the stack. </remarks>
        public void SetSwitchTargets(CilGraphNode[] newtargets)
        {
            this._SwitchTargets = newtargets;
        }
    }
}
