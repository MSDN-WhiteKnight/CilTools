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
        ClrAssemblyInfo assembly;

        public ClrTypeInfo(ClrType t, ClrAssemblyInfo ass)
        {
            Debug.Assert(t != null, "t in ClrTypeInfo() should not be null");
            Debug.Assert(ass != null, "ass in ClrTypeInfo() should not be null");

            this.type = t;
            this.assembly = ass;

            if (type.BaseType == null) this.basetype=null;
            else this.basetype = new ClrTypeInfo(type.BaseType, new ClrAssemblyInfo(null));

            if ((type.IsArray || type.IsPointer) && type.ComponentType!=null)
            {
                this.elemType = new ClrTypeInfo(type.ComponentType, new ClrAssemblyInfo(null));
            }
            else this.elemType = null;
        }

        public ClrType InnerType { get { return this.type; } }

        public override Assembly Assembly
        {
            get { return this.assembly; }
        }

        public override string AssemblyQualifiedName
        {
            get { return this.type.Name+", "+this.assembly.FullName; }
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
            TypeAttributes ret=(TypeAttributes)0;
            if (type.IsAbstract) ret |= TypeAttributes.Abstract;            
            if (type.IsInterface) ret |= TypeAttributes.Interface;
            if (type.IsPublic) ret |= TypeAttributes.Public;
            if (type.IsSealed) ret |= TypeAttributes.Sealed;
            return ret;
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
            throw new NotSupportedException("This type implementation does not support events");
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException("This type implementation does not support events");
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

            /*List<MemberInfo> members = new List<MemberInfo>();

            foreach (MemberInfo m in this.assembly.EnumerateMembers())
            {
                if (!String.Equals(m.DeclaringType.Name, this.type.Name, StringComparison.InvariantCulture)) continue;
                                
                members.Add(m);                
            }

            return members.ToArray();*/
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
            throw new NotSupportedException("This type implementation does not support properties");
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, 
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException("This type implementation does not support properties");
        }

        protected override bool HasElementTypeImpl()
        {
            return type.IsArray || type.IsPointer;
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            throw new InvalidOperationException("Cannot invoke members on type loaded into reflection-only context");
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
            get 
            {
                string tn = type.Name;
                int index = tn.LastIndexOf('.');

                if (index < 0) return "";
                else return tn.Substring(0, index);                
            }
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
            get 
            {
                string tn = type.Name;
                int index = tn.LastIndexOf('.');

                if (index < 0) return tn;
                if (index + 1 >= tn.Length) return tn;
                else return tn.Substring(index + 1);
            }
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
