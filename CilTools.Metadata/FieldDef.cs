﻿/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    class FieldDef : FieldInfo, IReflectionInfo
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
                    return owner.GetTypeDefinition(htdef);
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
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.field.GetCustomAttributes();
            return Utils.ReadCustomAttributes(coll, this, this.owner);
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

        internal static object GetConstantValue(MetadataReader reader, ConstantHandle hc)
        {
            Constant c = reader.GetConstant(hc);
            byte[] rawval = reader.GetBlobBytes(c.Value);
            MemoryStream ms = new MemoryStream(rawval);
            BinaryReader rd = new BinaryReader(ms);

            if (c.TypeCode == ConstantTypeCode.NullReference)
            {
                return null;
            }
            else if (c.TypeCode == ConstantTypeCode.Int32)
            {
                return rd.ReadInt32();
            }
            else if (c.TypeCode == ConstantTypeCode.UInt32)
            {
                return rd.ReadUInt32();
            }
            else if (c.TypeCode == ConstantTypeCode.String)
            {
                return Encoding.Unicode.GetString(rawval);
            }
            else if (c.TypeCode == ConstantTypeCode.Char)
            {
                return rd.ReadChar();
            }
            else if (c.TypeCode == ConstantTypeCode.Boolean)
            {
                return rd.ReadBoolean();
            }
            else if (c.TypeCode == ConstantTypeCode.Byte)
            {
                return rd.ReadByte();
            }
            else if (c.TypeCode == ConstantTypeCode.SByte)
            {
                return rd.ReadSByte();
            }
            else if (c.TypeCode == ConstantTypeCode.Int16)
            {
                return rd.ReadInt16();
            }
            else if (c.TypeCode == ConstantTypeCode.UInt16)
            {
                return rd.ReadUInt16();
            }
            else if (c.TypeCode == ConstantTypeCode.Int64)
            {
                return rd.ReadInt64();
            }
            else if (c.TypeCode == ConstantTypeCode.UInt64)
            {
                return rd.ReadUInt64();
            }
            else if (c.TypeCode == ConstantTypeCode.Single)
            {
                return rd.ReadSingle();
            }
            else if (c.TypeCode == ConstantTypeCode.Double)
            {
                return rd.ReadDouble();
            }
            else return DBNull.Value;
        }

        public override object GetRawConstantValue()
        {
            ConstantHandle ch = field.GetDefaultValue();
            if (ch.IsNil) return DBNull.Value;

            return GetConstantValue(owner.MetadataReader, ch);
        }

        int GetRVA()
        {
            if (this.field.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
            {
                return this.field.GetRelativeVirtualAddress();
            }
            else return 0;
        }

        byte[] GetRVABytes()
        {
            // Get byte array that contains field value, for fields that are mapped to static memory block 
            // at the specified address in PE image (RVA fields)

            if (!this.field.Attributes.HasFlag(FieldAttributes.HasFieldRVA)) return new byte[0];

            int rva = this.field.GetRelativeVirtualAddress();
            int size;

            if (this.FieldType is TypeSpec)
            {
                TypeSpec ts = (TypeSpec)this.FieldType;

                switch (ts.ElementType)
                {
                    case ElementType.Boolean: size = 1;break;
                    case ElementType.I1: size = 1; break;
                    case ElementType.U1: size = 1; break;
                    case ElementType.I2: size = 2; break;
                    case ElementType.U2: size = 2; break;
                    case ElementType.I4: size = 4;break;
                    case ElementType.U4: size = 4; break;
                    case ElementType.I8: size = 8; break;
                    case ElementType.U8: size = 8; break;
                    case ElementType.ValueType:
                        StructLayoutAttribute sla = ts.StructLayoutAttribute;

                        if (sla != null) size = sla.Size;
                        else return new byte[0];

                        break;
                    default: return new byte[0];
                }
            }
            else return new byte[0];

            PEMemoryBlock block = this.owner.PEReader.GetSectionData(rva);

            if(block.Length < size) return new byte[0];

            ImmutableArray<byte> data = block.GetContent(0, size);
            return ImmutableArrayExtensions.ToArray(data);
        }

        public object GetReflectionProperty(int id)
        {
            switch (id)
            {
                case ReflectionProperties.FieldRva: return this.GetRVA();
                case ReflectionProperties.RvaFieldValue: return this.GetRVABytes();
                case ReflectionProperties.FieldOffset: return this.field.GetOffset();
                default: return null;
            }
        }
    }
}

