/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;

namespace CilTools.Metadata
{
    enum ComplexTypeKind 
    {
        SzArray = 1, //single-dimensional zero-based array
        ByRef = 2, //reference
        Pointer = 3 //unmanaged pointer
    }
    class ComplexType : Type
    {
        //complex type is a type constructed on demand based on another type defined in some assembly ("element type")
        //for example, int[] is an array type constructed from element type [mscorlib]System.Int32

        Type inner;
        ComplexTypeKind kind;

        public ComplexType(Type t, ComplexTypeKind k)
        {
            this.inner = t;
            this.kind = k;
        }

        public override Assembly Assembly
        {
            get
            {
                return this.inner.Assembly;
            }
        }

        public override string AssemblyQualifiedName
        {
            get { return this.FullName + ", " + this.Assembly.FullName; }
        }

        public override Type BaseType
        {
            get
            {
                if (this.kind == ComplexTypeKind.SzArray) return typeof(Array);
                else return null;
            }
        }

        public override string FullName
        {
            get { return this.Namespace + "." + this.Name; }
        }

        public override Guid GUID
        {
            get { return new Guid(); }
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            TypeAttributes ret = inner.Attributes;
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
            return this.inner;
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
            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return new FieldInfo[0];
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            throw new NotImplementedException();
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
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
            throw new NotSupportedException("This type implementation does not support properties");
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException("This type implementation does not support properties");
        }

        protected override bool HasElementTypeImpl()
        {
            return true;
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            throw new InvalidOperationException("Cannot invoke members on type loaded into reflection-only context");
        }

        protected override bool IsArrayImpl()
        {
            return this.kind == ComplexTypeKind.SzArray;
        }

        protected override bool IsByRefImpl()
        {
            return this.kind == ComplexTypeKind.ByRef;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return this.kind == ComplexTypeKind.Pointer;
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
                string tn = inner.Namespace;
                return tn;
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
                string tn = inner.Name;

                switch (this.kind)
                {
                    case ComplexTypeKind.SzArray:tn += "[]";break;
                    case ComplexTypeKind.ByRef: tn += "&"; break;
                    case ComplexTypeKind.Pointer: tn += "*"; break;
                }

                return tn;
            }
        }

        public override int MetadataToken
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int GetArrayRank()
        {
            if (this.kind == ComplexTypeKind.SzArray) return 1;
            else throw new NotSupportedException("Arrays with more then one dimension are not supported");
        }

        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        public override string ToString()
        {
            return this.FullName;
        }
    }
}
