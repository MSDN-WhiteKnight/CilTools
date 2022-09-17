/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;

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
        Parameter rawParameter;
        MetadataAssembly ass;

        public ParameterSpec(TypeSpec ts, int i, MemberInfo mi)
        {
            this.type = ts;
            this.pos = i;
            this.member = mi;
            this.name = "";
            this.defval = DBNull.Value;
        }

        public ParameterSpec(TypeSpec ts, Parameter p, MemberInfo mi, MetadataAssembly ownerAssembly)
        {
            this.type = ts;
            this.pos = p.SequenceNumber-1;
            this.member = mi;
            this.name = ownerAssembly.MetadataReader.GetString(p.Name);
            this.attrs = p.Attributes;
            this.rawParameter = p;
            this.ass = ownerAssembly;

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

            this.defval = FieldDef.GetConstantValue(ownerAssembly.MetadataReader, hc);
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

        public override bool HasDefaultValue
        {
            get
            {
                return this.defval != DBNull.Value;
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

        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.rawParameter.GetCustomAttributes();
            return Utils.ReadCustomAttributes(coll, this, this.ass);
        }
    }
}
