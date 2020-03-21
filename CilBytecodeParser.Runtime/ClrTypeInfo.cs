/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using CilBytecodeParser;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    class ClrTypeInfo : Type
    {
        ClrType type;
        ClrTypeInfo basetype;
        ClrTypeInfo elemType;

        public ClrTypeInfo(ClrType t)
        {
            Debug.Assert(t != null, "t in ClrTypeInfo(ClrType t) should not be null");

            this.type = t;

            if (type.BaseType == null) this.basetype=null;
            else this.basetype = new ClrTypeInfo(type.BaseType);

            if ((type.IsArray || type.IsPointer) && type.ComponentType!=null)
            {
                this.elemType = new ClrTypeInfo(type.ComponentType);
            }
            else this.elemType = null;
        }

        public override Assembly Assembly
        {
            get { return new ClrAssemblyInfo(type.Module); }
        }

        public override string AssemblyQualifiedName
        {
            get { return new ClrAssemblyInfo(type.Module).FullName; }
        }

        public override Type BaseType
        {
            get 
            {
                return this.basetype;
            }
        }

        public override string FullName
        {
            get { return type.Name; }
        }

        public override Guid GUID
        {
            get { return new Guid(); }
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return (TypeAttributes)0;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, 
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type GetElementType()
        {
            return this.elemType;
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            throw new NotImplementedException();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, 
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, 
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl()
        {
            return type.IsArray || type.IsPointer;
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl()
        {
            return type.IsArray;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return type.IsPointer;
        }

        protected override bool IsPrimitiveImpl()
        {
            return type.IsPrimitive;
        }

        public override Module Module
        {
            get { return null; }
        }

        public override string Namespace
        {
            get { return ""; }
        }

        public override Type UnderlyingSystemType
        {
            get { return null; }
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
            get { return type.Name; }
        }

        public override int MetadataToken
        {
            get
            {
                return (int)type.MetadataToken;
            }
        }

        public override int GetArrayRank()
        {
            if (this.IsArray)
            {
                if (type.ElementType == ClrElementType.SZArray) return 1;
                else throw new NotSupportedException("Multi-dimensional arrays or arrays with non-zero lower bound are not supported.");
            }
            else return 0;            
        }
    }
}
