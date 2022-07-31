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
using CilTools.Internal;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    class TypeRef : Type
    {
        TypeReference tref;
        TypeReferenceHandle htref;
        MetadataAssembly assembly;
        Type impl = null;

        public TypeRef(TypeReference t, TypeReferenceHandle ht, MetadataAssembly ass)
        {
            Debug.Assert(ass != null, "ass in TypeRef() should not be null");

            this.tref = t;
            this.htref = ht;
            this.assembly = ass;
        }

        void LoadImpl()
        {
            //loads actual implementation type referenced by this instance

            if (this.impl != null) return;//already loaded
            if (this.assembly.AssemblyReader == null) return;

            Type et = this;
            Type t = this.assembly.AssemblyReader.LoadType(et);
            this.impl = t;
        }

        public override Assembly Assembly
        {
            get 
            {
                EntityHandle eh = this.tref.ResolutionScope;
                if (eh.IsNil) return MetadataAssembly.UnknownAssembly;

                if (eh.Kind == HandleKind.AssemblyReference)
                {
                    AssemblyReference ar = assembly.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)eh);
                    return new AssemblyRef(ar, (AssemblyReferenceHandle)eh, this.assembly);
                }
                else if (eh.Kind == HandleKind.TypeReference)
                {
                    TypeReference parent = assembly.MetadataReader.GetTypeReference((TypeReferenceHandle)eh);
                    if (parent.ResolutionScope.IsNil) return MetadataAssembly.UnknownAssembly;
                    if (parent.ResolutionScope.Kind!=HandleKind.AssemblyReference) return MetadataAssembly.UnknownAssembly;

                    AssemblyReference ar = assembly.MetadataReader.GetAssemblyReference(
                        (AssemblyReferenceHandle)parent.ResolutionScope
                        );

                    return new AssemblyRef(ar, (AssemblyReferenceHandle)parent.ResolutionScope, this.assembly);
                }
                else return MetadataAssembly.UnknownAssembly;
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
                this.LoadImpl();

                if (this.impl == null)
                {
                    throw new TypeLoadException("Failed to load base type");
                }

                return this.impl.BaseType;
            }
        }

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

                if (this.DeclaringType!=null)
                {
                    sb.Append(this.DeclaringType.Name);
                    sb.Append('+');
                }

                sb.Append(this.Name);
                return sb.ToString();
            }
        }

        public override Guid GUID
        {
            get { return new Guid(); }
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            TypeAttributes ret = (TypeAttributes)0;
            this.LoadImpl();
            if (this.impl != null) ret = this.impl.Attributes;
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
            return null;
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
            return new MemberInfo[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return new MemberInfo[0];
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetMethods(bindingAttr);
            else throw new TypeLoadException("Failed to load referenced type");
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
                string tn = assembly.MetadataReader.GetString(tref.Namespace);

                if (String.IsNullOrEmpty(tn))
                {
                    Type dt = this.DeclaringType;
                    if(dt!=null)tn = this.DeclaringType.Namespace;
                }

                return tn;
            }
        }

        public override Type UnderlyingSystemType
        {
            get 
            {
                if (this.Assembly == null) return null;

                //for corlib types try to fetch runtime type

                AssemblyName an = this.Assembly.GetName();
                Assembly corlib = typeof(object).Assembly;
                Type ret = null;

                if (String.Equals(an.Name, corlib.GetName().Name, StringComparison.InvariantCulture))
                {
                    ret = corlib.GetType(this.FullName);
                }

                return ret; 
            }
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
                string tn = assembly.MetadataReader.GetString(tref.Name);
                return tn;
            }
        }

        public override int MetadataToken
        {
            get
            {
                return assembly.MetadataReader.GetToken(this.htref);
            }
        }

        public override int GetArrayRank()
        {
            return 0;
        }

        public override Type MakeArrayType()
        {
            return new ComplexType(this, ComplexTypeKind.SzArray,null);
        }

        public override Type MakeByRefType()
        {
            return new ComplexType(this, ComplexTypeKind.ByRef, null);
        }

        public override Type MakePointerType()
        {
            return new ComplexType(this, ComplexTypeKind.Pointer, null);
        }

        public override Type MakeGenericType(params Type[] typeArguments)
        {
            return new ComplexType(this, ComplexTypeKind.GenInst, typeArguments);
        }

        public override Type DeclaringType
        {
            get
            {
                EntityHandle eh = this.tref.ResolutionScope;
                if (eh.IsNil) return null;

                if (eh.Kind == HandleKind.TypeReference)
                {
                    TypeReference parent = assembly.MetadataReader.GetTypeReference((TypeReferenceHandle)eh);
                    return new TypeRef(parent, (TypeReferenceHandle)eh, this.assembly);
                }
                else return null;
            }
        }

        public override bool IsAssignableFrom(Type c)
        {
            if (c.IsInterface || this.IsInterface)
            {
                throw new NotImplementedException("Interface checks are not implemented");
            }

            //check that c is derived directly or indirectly from current instance

            Type basetype = c;

            while (true)
            {
                if (Utils.TypeEquals(this, basetype)) return true;

                if (basetype == null) break;

                basetype = basetype.BaseType;
            }

            return false;
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

