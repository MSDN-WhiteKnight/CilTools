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
using CilTools.Reflection;

namespace CilTools.Metadata
{
    class TypeDef : Type
    {
        TypeDefinition type;
        TypeDefinitionHandle htype;
        MetadataAssembly assembly;
        PropertyInfo[] properties;

        public TypeDef(TypeDefinition t, TypeDefinitionHandle ht,MetadataAssembly ass)
        {
            Debug.Assert(ass != null, "ass in TypeDef() should not be null");

            this.type = t;
            this.htype = ht;
            this.assembly = ass;
        }

        void ThrowIfDisposed()
        {
            if (this.assembly.MetadataReader == null)
            {
                throw new ObjectDisposedException("MetadataReader");
            }
        }

        bool IsMemberMatching(MemberInfo m, BindingFlags bindingAttr)
        {
            bool access_match = false;
            bool sem_match = false;

            if (m is FieldInfo)
            {
                FieldInfo fi = (FieldInfo)m;

                if (bindingAttr.HasFlag(BindingFlags.Public) && fi.IsPublic) access_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.NonPublic) && !fi.IsPublic) access_match = true;

                if (bindingAttr.HasFlag(BindingFlags.Static) && fi.IsStatic) sem_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.Instance) && !fi.IsStatic) sem_match = true;
            }
            else if (m is MethodBase)
            {
                MethodBase mb = (MethodBase)m;

                if (bindingAttr.HasFlag(BindingFlags.Public) && mb.IsPublic) access_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.NonPublic) && !mb.IsPublic) access_match = true;

                if (bindingAttr.HasFlag(BindingFlags.Static) && mb.IsStatic) sem_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.Instance) && !mb.IsStatic) sem_match = true;
            }
            else if (m is PropertyInfo)
            {
                //filtering is not implemented
                if (bindingAttr != BindingFlags.Default)
                {
                    access_match = true;
                    sem_match = true;
                }
            }
            else if (m is Type)
            {
                Type t = (Type)m;

                if (bindingAttr.HasFlag(BindingFlags.Public) && t.IsPublic) access_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.NonPublic) && !t.IsPublic) access_match = true;

                sem_match = true;
            }

            return (access_match && sem_match);
        }

        public override Assembly Assembly
        {
            get { return this.assembly; }
        }

        public override string AssemblyQualifiedName
        {
            get { return this.FullName + ", " + this.assembly.FullName; }
        }

        public override Type BaseType
        {
            get
            {
                EntityHandle eh = type.BaseType;
                if (eh.IsNil) return null;
                Type ret = null;

                if (eh.Kind == HandleKind.TypeDefinition)
                {
                    TypeDefinitionHandle tdh = (TypeDefinitionHandle)eh;
                    ret = this.assembly.GetTypeDefinition(tdh);
                }
                else if (eh.Kind == HandleKind.TypeReference)
                {
                    TypeReferenceHandle trh = (TypeReferenceHandle)eh;
                    TypeReference btype = this.assembly.MetadataReader.GetTypeReference(trh);
                    ret = new TypeRef(btype, trh, this.assembly);
                }

                //prefer returning runtime type, when it's available for proper 
                //IsValueType functioning, because it compares with System.ValueType
                if (ret != null)
                {
                    if (ret.UnderlyingSystemType != null) ret = ret.UnderlyingSystemType;
                }

                return ret;
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

                if (this.IsNested)
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
            TypeAttributes ret = type.Attributes;
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
            FieldInfo[] fields = this.GetFields(bindingAttr);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo m = fields[i];

                if (String.Equals(m.Name, name, StringComparison.InvariantCulture)) return (FieldInfo)m;
            }

            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            List<FieldInfo> members = new List<FieldInfo>();
            MemberInfo m;

            foreach (FieldDefinitionHandle hfield in this.type.GetFields())
            {
                FieldDefinition field = this.assembly.MetadataReader.GetFieldDefinition(hfield);
                m = new FieldDef(field, hfield, this.assembly,null);
                if (IsMemberMatching(m, bindingAttr)) members.Add((FieldInfo)m);
            }

            return members.ToArray();
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
            MemberInfo[] members = this.GetMembers(bindingAttr);
            List<MemberInfo> ret = new List<MemberInfo>();

            for(int i=0;i<members.Length;i++)
            {
                MemberInfo m = members[i];

                if (String.Equals(m.Name, name, StringComparison.InvariantCulture)) ret.Add(m);
            }

            return ret.ToArray();
        }

        PropertyInfo[] LoadProperties()
        {
            List<PropertyInfo> props = new List<PropertyInfo>();

            foreach (PropertyDefinitionHandle hp in this.type.GetProperties())
            {
                PropertyDefinition pd = this.assembly.MetadataReader.GetPropertyDefinition(hp);
                PropertyInfo p = new PropertyDef(pd, hp, this.assembly, this);
                props.Add(p);
            }

            return props.ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            MemberInfo m;

            foreach (MethodDefinitionHandle mdefh in this.type.GetMethods())
            {
                m = this.assembly.GetMethodDefinition(mdefh);
                if (IsMemberMatching(m, bindingAttr)) members.Add(m);
            }

            foreach (TypeDefinitionHandle ht in this.type.GetNestedTypes())
            {
                m = this.assembly.GetTypeDefinition(ht);
                if (IsMemberMatching(m, bindingAttr)) members.Add(m);
            }

            foreach (FieldDefinitionHandle hfield in this.type.GetFields())
            {
                FieldDefinition field = this.assembly.MetadataReader.GetFieldDefinition(hfield);
                m = new FieldDef(field, hfield, this.assembly, null);
                if (IsMemberMatching(m, bindingAttr)) members.Add(m);
            }

            //cache properties so their lookup does not slow down every GetMembers() call
            //we can cache them all the time, because attributes filtering is not implemented anyway
            if (this.properties == null) this.properties = this.LoadProperties();

            if (bindingAttr != BindingFlags.Default)
            {
                for (int i = 0; i < this.properties.Length; i++)
                {
                    members.Add(this.properties[i]);
                }
            }

            return members.ToArray();
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
            List<Type> members = new List<Type>();
            MemberInfo m;

            foreach (TypeDefinitionHandle ht in this.type.GetNestedTypes())
            {
                m = this.assembly.GetTypeDefinition(ht);
                if (IsMemberMatching(m, bindingAttr)) members.Add((Type)m);
            }

            return members.ToArray();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            //cache properties so their lookup does not slow down every GetMembers() call
            //we can cache them all the time, because attributes filtering is not implemented anyway
            if (this.properties == null) this.properties = this.LoadProperties();

            if (bindingAttr != BindingFlags.Default) return this.properties;
            else return new PropertyInfo[0];
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            PropertyInfo[] props = this.GetProperties(bindingAttr);

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                if (StrEquals(p.Name, name)) return p;
            }

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
                this.ThrowIfDisposed();
                string tn = assembly.MetadataReader.GetString(type.Namespace);

                if (String.IsNullOrEmpty(tn) && this.IsNested)
                {
                    Type dt = this.DeclaringType;
                    if(dt!=null)tn = this.DeclaringType.Namespace;
                }

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

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.type.GetCustomAttributes();
            
            return MethodDef.ReadCustomAttributes(coll, this, this.assembly);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override string Name
        {
            get
            {
                this.ThrowIfDisposed();
                string tn=assembly.MetadataReader.GetString(type.Name);
                return tn;
            }
        }

        public override int MetadataToken
        {
            get
            {
                return assembly.MetadataReader.GetToken(this.htype);
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

        public override bool IsGenericType
        {
            get
            {
                GenericParameterHandleCollection hcoll = type.GetGenericParameters();
                return hcoll.Count > 0;
            }
        }

        public override bool IsGenericTypeDefinition => this.IsGenericType;

        public override Type[] GetGenericArguments()
        {
            GenericParameterHandleCollection hcoll = type.GetGenericParameters();
            Type[] ret = new Type[hcoll.Count];

            for (int i = 0; i < ret.Length; i++)
            {
                GenericParameter gp = this.assembly.MetadataReader.GetGenericParameter(hcoll[i]);
                StringHandle sh = gp.Name;

                if (!sh.IsNil)
                    ret[i] = GenericParamType.Create(this, gp.Index, assembly.MetadataReader.GetString(sh));
                else
                    ret[i] = GenericParamType.Create(this, gp.Index, string.Empty);
            }

            return ret;
        }

        public override int GetHashCode()
        {
            if (this.assembly.MetadataReader == null)
            {
                return 0;
            }

            return this.FullName.GetHashCode();
        }

        public override Type DeclaringType
        {
            get
            {
                TypeDefinitionHandle ht = this.type.GetDeclaringType();

                if (ht.IsNil) return null;

                return this.assembly.GetTypeDefinition(ht);
            }
        }

        internal static bool StrEquals(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCulture);
        }

        internal static bool TypeEquals(Type left, Type right)
        {
            if (Type.ReferenceEquals(left,right)) return true;

            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if(right==null) return false;

            string left_assname = String.Empty;
            string right_assname = String.Empty;

            if (left.Assembly != null) left_assname = left.Assembly.GetName().Name;
            if (right.Assembly != null) right_assname = right.Assembly.GetName().Name;

            return StrEquals(left_assname,right_assname) && StrEquals(left.FullName,right.FullName);
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
                if (TypeEquals(this, basetype)) return true;

                if (basetype == null) break;

                basetype = basetype.BaseType;
            }

            return false;
        }

        public override string ToString()
        {
            //check if disposed
            if (this.assembly.MetadataReader == null) return "(TypeDef)";

            //return full name
            string ret = this.FullName;
            if (String.IsNullOrEmpty(ret)) ret = "(TypeDef)";
            return ret;
        }
    }
}
