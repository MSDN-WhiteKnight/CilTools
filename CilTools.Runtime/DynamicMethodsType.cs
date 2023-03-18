/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    class DynamicMethodsType : Type
    {
        internal const string TypeName = "<DynamicMethods>";

        DynamicMethodsAssembly owner;

        public DynamicMethodsType(DynamicMethodsAssembly o)
        {
            this.owner = o;
        }

        public override Assembly Assembly
        {
            get { return this.owner; }
        }

        public override string FullName
        {
            get { return TypeName; }
        }

        public override string AssemblyQualifiedName
        {
            get { return this.FullName + ", " + this.owner.FullName; }
        }

        public override Type BaseType
        {
            get
            {
                return typeof(object);
            }
        }

        public override Guid GUID
        {
            get { return new Guid(); }
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            TypeAttributes ret = (TypeAttributes)0;
            ret |= TypeAttributes.NotPublic;
            ret |= TypeAttributes.Sealed;
            return ret;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            return null;
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return new ConstructorInfo[] { };
        }

        public override Type GetElementType()
        {
            return null;
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return new EventInfo[] { };
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return new FieldInfo[] { };
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            return null;
        }

        public override Type[] GetInterfaces()
        {
            return new Type[] { };
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            if (!bindingAttr.HasFlag(BindingFlags.Public)) return new MethodInfo[0];
            if (!bindingAttr.HasFlag(BindingFlags.Static)) return new MethodInfo[0];

            List<MethodInfo> ret = new List<MethodInfo>();

            foreach (MethodBase m in this.owner.EnumerateMethods())
            {
                if(m is MethodInfo) ret.Add((MethodInfo)m);
            }

            return ret.ToArray();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return new Type[] { };
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return new PropertyInfo[] { };
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            return null;
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            throw new InvalidOperationException("Cannot invoke members on type loaded into reflection-only context");
        }

        protected override bool IsArrayImpl()
        {
            return false;
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
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        public override Module Module
        {
            get { return null; }
        }

        public override string Namespace
        {
            get
            {
                return "";
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
                return TypeName;
            }
        }

        public override StructLayoutAttribute StructLayoutAttribute => null;

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (!bindingAttr.HasFlag(BindingFlags.Public)) return new MemberInfo[] { };
            if (!bindingAttr.HasFlag(BindingFlags.Static)) return new MemberInfo[] { };
            
            List<MemberInfo> members = new List<MemberInfo>();

            foreach (MethodBase m in this.owner.EnumerateMethods())
            {                                
                members.Add(m);                
            }

            return members.ToArray();
        }

        public override int GetHashCode()
        {
            return owner.FullName.GetHashCode();
        }
    }
}
