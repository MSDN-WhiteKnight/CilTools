/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    class PropertyDef : PropertyInfo
    {
        PropertyDefinition prop;
        PropertyDefinitionHandle hprop;
        MetadataAssembly owner;
        TypeDef declaringType;

        public PropertyDef(PropertyDefinition p, PropertyDefinitionHandle ph, MetadataAssembly o,TypeDef dt)
        {
            this.prop = p;
            this.hprop = ph;
            this.owner = o;
            this.declaringType = dt;
        }

        void ThrowIfDisposed()
        {
            if (this.owner.MetadataReader == null)
            {
                throw new ObjectDisposedException("MetadataReader");
            }
        }

        public override PropertyAttributes Attributes => this.prop.Attributes;

        public override bool CanRead
        {
            get
            {
                this.ThrowIfDisposed();
                if (this.prop.GetAccessors().Getter.IsNil) return false;
                else return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                this.ThrowIfDisposed();
                if (this.prop.GetAccessors().Setter.IsNil) return false;
                else return true;
            }
        }

        public override Type PropertyType
        {
            get
            {
                this.ThrowIfDisposed();
                
                Signature decoded = Utils.DecodeSignature(this.owner, this.prop.Signature, this.declaringType);
                return decoded.ReturnType;
            }
        }

        public override Type DeclaringType => this.declaringType;

        public override string Name
        {
            get
            {
                this.ThrowIfDisposed();
                return this.owner.MetadataReader.GetString(this.prop.Name);
            }
        }

        public override Type ReflectedType => throw new NotImplementedException();

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.prop.GetCustomAttributes();

            return Utils.ReadCustomAttributes(coll, this, this.owner);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            this.ThrowIfDisposed();

            Signature decoded = Utils.DecodeSignature(this.owner, this.prop.Signature, this.declaringType);
            return Utils.GetParametersFromSignature(decoded,this);
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot access property values on type loaded into reflection-only context");
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot access property values on type loaded into reflection-only context");
        }
    }
}
