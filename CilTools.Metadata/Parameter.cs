/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;

namespace CilTools.Metadata
{
    class Parameter:ParameterInfo
    {
        TypeSpec type;
        int pos;
        MemberInfo member;

        public Parameter(TypeSpec ts, int i, MemberInfo mi)
        {
            this.type = ts;
            this.pos = i;
            this.member = mi;
        }

        public override Type ParameterType
        {
            get
            {
                return type.Type;
            }
        }

        public override int Position
        {
            get
            {
                return this.pos;
            }
        }

        public override string Name
        {
            get
            {
                return "p"+this.pos.ToString();
            }
        }

        public override MemberInfo Member
        {
            get
            {
                return this.member;
            }
        }
    }
}
