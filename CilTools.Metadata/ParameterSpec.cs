/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using System.Reflection.Metadata;

namespace CilTools.Metadata
{
    class ParameterSpec:ParameterInfo
    {
        TypeSpec type;
        int pos;
        MemberInfo member;
        string name;
        ParameterAttributes attrs;
        object defval;

        public ParameterSpec(TypeSpec ts, int i, MemberInfo mi)
        {
            this.type = ts;
            this.pos = i;
            this.member = mi;
            this.name = "";
            this.defval = DBNull.Value;
        }

        public ParameterSpec(TypeSpec ts, Parameter p, MemberInfo mi,MetadataReader reader)
        {
            this.type = ts;
            this.pos = p.SequenceNumber-1;
            this.member = mi;
            this.name = reader.GetString(p.Name);
            this.attrs = p.Attributes;

            if (!p.Attributes.HasFlag(ParameterAttributes.HasDefault)) //no default value
            {
                this.defval = DBNull.Value;
                return; 
            }

            ConstantHandle hc = p.GetDefaultValue();

            if (hc.IsNil) //no default value
            {
                this.defval = DBNull.Value;
                return;
            }

            this.defval = FieldDef.GetConstantValue(reader, hc);
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
                if (!String.IsNullOrEmpty(this.name)) return this.name;
                else return "par"+this.pos.ToString();
            }
        }

        public override MemberInfo Member
        {
            get
            {
                return this.member;
            }
        }

        public override object DefaultValue
        {
            get
            {
                return this.defval;
            }
        }

        public override object RawDefaultValue
        {
            get
            {
                return this.defval;
            }
        }

        public override ParameterAttributes Attributes
        {
            get
            {
                return this.attrs;
            }
        }
    }
}
