/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Metadata
{
    class FieldDef : FieldInfo
    {
        FieldDefinition field;
        FieldDefinitionHandle hfield;
        MetadataAssembly owner;
        MethodBase generic_context;

        public FieldDef(FieldDefinition f, FieldDefinitionHandle fh, MetadataAssembly o,MethodBase gctx)
        {
            this.field = f;
            this.hfield = fh;
            this.owner = o;
            this.generic_context = gctx;
        }

        public override FieldAttributes Attributes
        {
            get
            {
                FieldAttributes ret = field.Attributes;
                return ret;
            }
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type FieldType
        {
            get
            {
                byte[] sig = owner.MetadataReader.GetBlobBytes(field.Signature);
                TypeSpec ts = Signature.ReadFieldSignature(sig, owner,this.generic_context);

                if (ts != null) return ts.Type;
                else return UnknownType.Value;
            }
        }

        public override object GetValue(object obj)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override Type DeclaringType
        {
            get
            {
                TypeDefinitionHandle htdef = field.GetDeclaringType();

                if (!htdef.IsNil)
                {
                    return new TypeDef(owner.MetadataReader.GetTypeDefinition(htdef), htdef, this.owner);
                }
                else return null;
            }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new object[] { };
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[] { };
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override string Name
        {
            get { return owner.MetadataReader.GetString(field.Name); }
        }

        public override Type ReflectedType
        {
            get { return null; }
        }

        public override int MetadataToken
        {
            get
            {
                return owner.MetadataReader.GetToken(this.hfield);
            }
        }
    }
}

