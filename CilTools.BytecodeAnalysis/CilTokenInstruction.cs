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
    sealed class CilTokenInstruction:CilInstruction<int>
    {
        public CilTokenInstruction(
            OpCode opc, int operand, uint operandsize, uint byteoffset = 0, uint ordinalnum = 0, MethodBase mb = null
            )
            : base(opc,operand,operandsize, byteoffset, ordinalnum, mb)
        {
            //do nothing
        }

        public override MemberInfo ReferencedMember
        {
            get
            {
                try
                {
                    return ResolveMemberToken(this._operand);
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
