/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilBytecodeParser;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    class ClrFieldInfo : FieldInfo
    {
        ClrField field;
        Type fieldtype;
        ClrTypeInfo ownertype;

        public ClrFieldInfo(ClrField f, ClrTypeInfo owner)
        {
            this.field = f;
            this.ownertype = owner;
            ClrType ft = field.Type;

            if (ft == null) this.fieldtype = UnknownType.Value;
            else this.fieldtype = new ClrTypeInfo(ft, (ClrAssemblyInfo)owner.Assembly); //TODO: Deduplicate ClrTypeInfo instances
        }

        public ClrField InnerField { get { return this.field; } }

        public override FieldAttributes Attributes
        {
            get 
            {
                FieldAttributes ret = (FieldAttributes)0;
                if (field.IsInternal) ret |= FieldAttributes.Assembly;
                if (field.IsProtected) ret |= FieldAttributes.Family;
                if (field.IsPrivate) ret |= FieldAttributes.Private;
                if (field.IsPublic) ret |= FieldAttributes.Public;
                if (field is ClrStaticField) ret |= FieldAttributes.Static;                
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
                return this.fieldtype;
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
            get { return this.ownertype; }
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
            get { return field.Name; }
        }

        public override Type ReflectedType
        {
            get { return UnknownType.Value; }
        }

        public override int MetadataToken
        {
            get
            {
                return (int)field.Token;
            }
        }
    }
}
