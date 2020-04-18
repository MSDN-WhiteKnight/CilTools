/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.Reflection;
using System.Diagnostics;

namespace CilTools.BytecodeAnalysis
{
    public sealed class CilInstruction : CilInstructionBase
    {
        public CilInstruction(
            OpCode opc, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
            : base(opc, byteoffset, ordinalnum, mb)
        {
            //do nothing
        }

        public override object Operand
        {
            get { return null; }
        }

        public override uint OperandSize
        {
            get { return 0; }
        }
                
        public override Type OperandType
        {
            get { return null; }
        }

        public override MemberInfo ReferencedMember
        {
            get { return null; }
        }
    }

    public class CilInstruction<T> : CilInstructionBase
    {
        protected T _operand;
        uint _operandsize;

        public CilInstruction(
            OpCode opc,T operand,uint operandsize,uint byteoffset=0, uint ordinalnum=0, MethodBase mb=null
            ):base(opc,byteoffset,ordinalnum,mb)
        {
            this._operand = operand;
            this._operandsize = operandsize;
        }

        public override object Operand
        {
            get { return (object)this._operand; }
        }

        public T OperandValue
        {
            get { return this._operand; }
        }

        public override uint OperandSize
        {
            get { return _operandsize; }
        }

        public override Type OperandType
        {
            get
            {
                return typeof(T);
            }
        }
    
        /// <summary>
        /// Gets a member (type, field or method) referenced by this instruction, if applicable
        /// </summary>
        public override MemberInfo ReferencedMember
        {
            get
            {
                if (typeof(T) != typeof(int)) return null;
                
                try
                {
                    return ResolveMemberToken((int)Operand);
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve member token.";
                    OnError(this, new CilErrorEventArgs(ex, error));
                    return null;
                }
            }
        }
    }
}
