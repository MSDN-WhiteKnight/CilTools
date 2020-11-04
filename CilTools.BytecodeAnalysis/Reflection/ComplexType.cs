/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Specifies the kind of the complex type
    /// </summary>
    public enum ComplexTypeKind
    {
        /// <summary>
        /// Single-dimensional zero-based array
        /// </summary>
        SzArray = 1, 

        /// <summary>
        /// Managed reference
        /// </summary>
        ByRef = 2, 

        /// <summary>
        /// Unmanaged pointer
        /// </summary>
        Pointer = 3,

        /// <summary>
        /// Generic type instantiation
        /// </summary>
        GenInst = 4, 
    }

    /// <summary>
    /// Represents a complex type. Complex type is a type contructed on demand based on another type defined in some assembly, 
    /// for example, an array or pointer type.
    /// </summary>
    public class ComplexType : Type
    {
        Type inner;
        ComplexTypeKind kind;
        Type[] genargs;

        /// <summary>
        /// Creates new instance of the complex type
        /// </summary>
        /// <param name="t">Element type</param>
        /// <param name="k">Kind of the complex type</param>
        /// <param name="ga">An array of generic type arguments, or null if this is not a generic type instantiation</param>
        public ComplexType(Type t, ComplexTypeKind k, Type[] ga)
        {
            this.inner = t;
            this.kind = k;
            this.genargs = ga;
        }

        /// <inheritdoc/>        
        public override Assembly Assembly
        {
            get
            {
                return this.inner.Assembly;
            }
        }

        /// <inheritdoc/>
        public override string AssemblyQualifiedName
        {
            get { return this.FullName + ", " + this.Assembly.FullName; }
        }

        /// <inheritdoc/>
        public override Type BaseType
        {
            get
            {
                if (this.kind == ComplexTypeKind.SzArray) return typeof(Array);
                else return null;
            }
        }

        /// <inheritdoc/>
        public override string FullName
        {
            get
            {
                StringBuilder sb = new StringBuilder(500);

                if (!String.IsNullOrEmpty(this.Namespace))
                {
                    sb.Append(this.Namespace);
                    sb.Append('.');
                }

                if (this.IsNested)
                {
                    sb.Append(this.DeclaringType.Name);
                    sb.Append('+');
                }

                sb.Append(this.Name);
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override Guid GUID
        {
            get { return new Guid(); }
        }

        /// <inheritdoc/>
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            TypeAttributes ret = inner.Attributes;
            return ret;
        }

        /// <inheritdoc/>
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
            }
            else return null;
        }

        /// <inheritdoc/>
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetConstructors(bindingAttr);
            }
            else return new ConstructorInfo[0];
        }

        /// <inheritdoc/>
        public override Type GetElementType()
        {
            if (this.kind != ComplexTypeKind.GenInst) return this.inner;
            else return null;
        }

        /// <inheritdoc/>
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException("This type implementation does not support events");
        }

        /// <inheritdoc/>
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException("This type implementation does not support events");
        }

        /// <inheritdoc/>
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetField(name, bindingAttr);
            }
            else return null;
        }

        /// <inheritdoc/>
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetFields(bindingAttr);
            }
            else return new FieldInfo[0];
        }

        /// <inheritdoc/>
        public override Type GetInterface(string name, bool ignoreCase)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetInterface(name, ignoreCase);
            }
            else return null;
        }

        /// <inheritdoc/>
        public override Type[] GetInterfaces()
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetInterfaces();
            }
            else return new Type[0];
        }

        /// <inheritdoc/>
        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMember(name, bindingAttr);
            }
            else return new MemberInfo[0];
        }

        /// <inheritdoc/>
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMembers(bindingAttr);
            }
            else return new MemberInfo[0];
        }

        /// <inheritdoc/>
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
            else return null;
        }

        /// <inheritdoc/>
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetMethods(bindingAttr);
            }
            else return new MethodInfo[0];
        }

        /// <inheritdoc/>
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetNestedType(name, bindingAttr);
            }
            else return null;
        }

        /// <inheritdoc/>
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetNestedTypes(bindingAttr);
            }
            else return new Type[0];
        }

        /// <inheritdoc/>
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotSupportedException("This type implementation does not support properties");
        }

        /// <inheritdoc/>
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException("This type implementation does not support properties");
        }

        /// <inheritdoc/>
        protected override bool HasElementTypeImpl()
        {
            if (this.kind != ComplexTypeKind.GenInst) return true;
            else return false;
        }

        /// <inheritdoc/>
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            throw new InvalidOperationException("Cannot invoke members on type loaded into reflection-only context");
        }

        /// <inheritdoc/>
        protected override bool IsArrayImpl()
        {
            return this.kind == ComplexTypeKind.SzArray;
        }

        /// <inheritdoc/>
        protected override bool IsByRefImpl()
        {
            return this.kind == ComplexTypeKind.ByRef;
        }

        /// <inheritdoc/>
        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        /// <inheritdoc/>
        protected override bool IsPointerImpl()
        {
            return this.kind == ComplexTypeKind.Pointer;
        }

        /// <inheritdoc/>
        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        /// <inheritdoc/>
        public override Module Module
        {
            get { return null; }
        }

        /// <inheritdoc/>
        public override string Namespace
        {
            get
            {
                string tn = inner.Namespace;
                return tn;
            }
        }

        /// <inheritdoc/>
        public override Type UnderlyingSystemType
        {
            get { return null; }
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetCustomAttributes(attributeType, inherit);
            }
            else return new object[0];
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.GetCustomAttributes(inherit);
            }
            else return new object[0];
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (this.kind == ComplexTypeKind.GenInst)
            {
                return this.inner.IsDefined(attributeType, inherit);
            }
            else return false;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                return this.inner.MetadataToken;
            }
        }

        /// <inheritdoc/>
        public override int GetArrayRank()
        {
            if (this.kind == ComplexTypeKind.SzArray) return 1;
            else throw new NotSupportedException("Arrays with more then one dimension are not supported.");
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool IsGenericType
        {
            get
            {
                if (this.kind == ComplexTypeKind.GenInst) return true;
                else return false;
            }
        }

        /// <inheritdoc/>
        public override bool IsGenericTypeDefinition
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override Type GetGenericTypeDefinition()
        {
            if (this.kind == ComplexTypeKind.GenInst) return this.inner;
            else return null;
        }

        /// <inheritdoc/>
        public override Type[] GetGenericArguments()
        {
            return this.genargs;
        }

        /// <inheritdoc/>
        public override Type MakeArrayType()
        {
            return new ComplexType(this, ComplexTypeKind.SzArray, null);
        }

        /// <inheritdoc/>
        public override Type MakeByRefType()
        {
            return new ComplexType(this, ComplexTypeKind.ByRef, null);
        }

        /// <inheritdoc/>
        public override Type MakePointerType()
        {
            return new ComplexType(this, ComplexTypeKind.Pointer, null);
        }

        /// <inheritdoc/>
        public override Type DeclaringType
        {
            get
            {
                return this.inner.DeclaringType;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.FullName;
        }
    }
}
