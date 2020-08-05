/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    public enum ComplexTypeKind
    {
        SzArray = 1, //single-dimensional zero-based array
        ByRef = 2, //reference
        Pointer = 3, //unmanaged pointer
        GenInst = 4, //generic type instantiation
    }

    public class ComplexType : Type
    {
        //complex type is a type constructed on demand based on another type defined in some assembly
        //for example, int[] is an array type constructed from element type [mscorlib]System.Int32

        Type inner;
        ComplexTypeKind kind;
        Type[] genargs;

        public ComplexType(Type t, ComplexTypeKind k, Type[] ga)
        {
            this.inner = t;
            this.kind = k;
            this.genargs = ga;
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
            get
            {
                if (!String.IsNullOrEmpty(this.Namespace)) return this.Namespace + "." + this.Name;
                else return this.Name;
            }
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
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
            }
            else return null;
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetConstructors(bindingAttr);
            }
            else return new ConstructorInfo[0];
        }

        public override Type GetElementType()
        {
            if (this.kind != ComplexTypeKind.GenInst) return this.inner;
            else return null;
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
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetField(name, bindingAttr);
            }
            else return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetFields(bindingAttr);
            }
            else return new FieldInfo[0];
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetInterface(name, ignoreCase);
            }
            else return null;
        }

        public override Type[] GetInterfaces()
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetInterfaces();
            }
            else return new Type[0];
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMember(name, bindingAttr);
            }
            else return new MemberInfo[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMembers(bindingAttr);
            }
            else return new MemberInfo[0];
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
            else return null;
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMethods(bindingAttr);
            }
            else return new MethodInfo[0];
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetNestedType(name, bindingAttr);
            }
            else return null;
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetNestedTypes(bindingAttr);
            }
            else return new Type[0];
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
            if (this.kind != ComplexTypeKind.GenInst) return true;
            else return false;
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
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetCustomAttributes(attributeType, inherit);
            }
            else return new object[0];
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetCustomAttributes(inherit);
            }
            else return new object[0];
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.IsDefined(attributeType, inherit);
            }
            else return false;
        }

        public override string Name
        {
            get
            {
                string tn = inner.Name;

                switch (this.kind)
                {
                    case ComplexTypeKind.SzArray: tn += "[]"; break;
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
            else throw new NotSupportedException("Arrays with more then one dimension are not supported.");
        }

        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        public override bool IsGenericType
        {
            get
            {
                if (this.kind == ComplexTypeKind.GenInst) return true;
                else return false;
            }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                return false;
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            if (this.kind == ComplexTypeKind.GenInst) return this.inner;
            else return null;
        }

        public override Type[] GetGenericArguments()
        {
            return this.genargs;
        }

        public override Type MakeArrayType()
        {
            return new ComplexType(this, ComplexTypeKind.SzArray, null);
        }

        public override Type MakeByRefType()
        {
            return new ComplexType(this, ComplexTypeKind.ByRef, null);
        }

        public override Type MakePointerType()
        {
            return new ComplexType(this, ComplexTypeKind.Pointer, null);
        }

        public override Type DeclaringType
        {
            get
            {
                return this.inner.DeclaringType;
            }
        }

        public override string ToString()
        {
            return this.FullName;
        }
    }
}
