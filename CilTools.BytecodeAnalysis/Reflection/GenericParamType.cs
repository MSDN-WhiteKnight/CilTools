/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents the generic parameter
    /// </summary>
    public class GenericParamType : Type
    {
        MemberInfo _m;
        int _index;
        string _name;

        /// <summary>
        /// Creates a new instance of the generic parameter
        /// </summary>
        /// <param name="m">Declaring method, if this is a generic method parameter</param>
        /// <param name="index">Generic parameter index</param>
        public GenericParamType(MethodBase m, int index):this((MemberInfo)m, index, null)
        {

        }

        /// <summary>
        /// Creates a new instance of the generic parameter
        /// </summary>
        /// <param name="declaringMember">Declaring generic type or method of this parameter</param>
        /// <param name="index">Generic parameter index</param>
        /// <param name="name">Generic parameter name, or null to fill name automatically</param>
        public static GenericParamType Create(MemberInfo declaringMember, int index, string name)
        {
            return new GenericParamType(declaringMember, index, name);
        }
        
        GenericParamType(MemberInfo declaringMember, int index, string name)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index", "generic parameter index should be non-negative");

            this._m = declaringMember;
            this._index = index;

            if (name!=null)
            {
                //if name is provided, use it
                this._name = name;
                return;
            }

            Type[] args = null;
            Type t = null;
            MethodBase m = declaringMember as MethodBase;

            //try load parameter name from declaring member
            if (declaringMember!=null)
            {
                args = GenericContext.TryGetGenericArguments(declaringMember);
            }

            if (args != null && index<args.Length)
            {
                t = args[index];
            }

            if (t != null && t.IsGenericParameter)
            {
                this._name = t.Name;
            }
            else
            {
                this._name = "";
            }
        }

        /// <summary>
        /// Creates a new instance of the generic parameter with the specified name
        /// </summary>
        /// <param name="m">Declaring method, if this is a generic method parameter</param>
        /// <param name="index">Generic parameter index</param>
        /// <param name="name">Generic parameter name</param>
        public GenericParamType(MethodBase m, int index,string name)
        {
            this._m = m;
            this._index = index;
            this._name = name;
        }

        /// <inheritdoc/>
        public override Assembly Assembly
        {
            get { return null; }
        }

        /// <inheritdoc/>
        public override string AssemblyQualifiedName
        {
            get { return ""; }
        }

        /// <inheritdoc/>
        public override Type BaseType
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override string FullName
        {
            get { return this.Name; }
        }

        /// <inheritdoc/>
        public override Guid GUID
        {
            get { return new Guid(); }
        }

        /// <inheritdoc/>
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            TypeAttributes ret = (TypeAttributes)0;
            return ret;
        }

        /// <inheritdoc/>
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Type GetElementType()
        {
            return null;
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Type[] GetInterfaces()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
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
            return false;
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
            return false;
        }

        /// <inheritdoc/>
        protected override bool IsByRefImpl()
        {
            return false;
        }

        /// <inheritdoc/>
        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        /// <inheritdoc/>
        protected override bool IsPointerImpl()
        {
            return false;
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
                return "";
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
            return new object[] { };
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[] { };
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                return this._name;
            }
        }

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public override int GetArrayRank()
        {
            return 0;
        }

        /// <inheritdoc/>
        public override bool IsGenericParameter
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc/>
        public override int GenericParameterPosition
        {
            get
            {
                return this._index;
            }
        }

        /// <inheritdoc/>
        public override MethodBase DeclaringMethod
        {
            get
            {
                return this._m as MethodBase;
            }
        }

        /// <inheritdoc/>
        public override Type DeclaringType
        {
            get
            {
                return this._m as Type;
            }
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
    }
}
