/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Metadata
{
    class ExternalField : FieldInfo
    {
        MemberReference field;
        MemberReferenceHandle hfield;
        MetadataAssembly owner;

        public ExternalField(MemberReference f, MemberReferenceHandle fh, MetadataAssembly o)
        {
            Debug.Assert(f.GetKind() == MemberReferenceKind.Field, "MemberReference passed to ExternalField ctor should be a field");

            this.field = f;
            this.hfield = fh;
            this.owner = o;
        }

        public override FieldAttributes Attributes
        {
            get
            {
                FieldAttributes ret = (FieldAttributes)0;
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
                TypeSpec ts = Signature.ReadFieldSignature(sig, owner);

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
                EntityHandle eh = field.Parent;

                if (!eh.IsNil && eh.Kind == HandleKind.TypeReference)
                {
                    return new ExternalType(owner.MetadataReader.GetTypeReference((TypeReferenceHandle)eh), (TypeReferenceHandle)eh, this.owner);
                }
                else if (!eh.IsNil && eh.Kind == HandleKind.TypeSpecification)
                {
                    //TypeSpec is either complex type (array etc.) or generic instantiation

                    TypeSpecification ts = this.owner.MetadataReader.GetTypeSpecification(
                        (TypeSpecificationHandle)eh
                        );

                    TypeSpec encoded = TypeSpec.ReadFromArray(owner.MetadataReader.GetBlobBytes(ts.Signature),
                        this.owner,
                        null);

                    if (encoded != null) return encoded.Type;
                    else return UnknownType.Value;
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


