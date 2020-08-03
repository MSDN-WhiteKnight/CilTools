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
    class MetadataType : Type
    {
        TypeDefinition type;
        TypeDefinitionHandle htype;
        MetadataAssembly assembly;                

        public MetadataType(TypeDefinition t, TypeDefinitionHandle ht,MetadataAssembly ass)
        {
            Debug.Assert(ass != null, "ass in MetadataType() should not be null");

            this.type = t;
            this.htype = ht;
            this.assembly = ass;
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
                return null;
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
            foreach (MemberInfo m in this.assembly.EnumerateMembers())
            {
                if (m.DeclaringType == null) continue;

                if (!String.Equals(m.DeclaringType.FullName, this.FullName, StringComparison.InvariantCulture)) continue;

                if (!String.Equals(m.Name, name, StringComparison.InvariantCulture)) continue;

                if (IsMemberMatching(m, bindingAttr) && m is FieldInfo) return (FieldInfo)m;
            }

            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            List<FieldInfo> members = new List<FieldInfo>();

            foreach (MemberInfo m in this.assembly.EnumerateMembers())
            {
                if (m.DeclaringType == null) continue;

                if (!String.Equals(m.DeclaringType.FullName, this.FullName, StringComparison.InvariantCulture)) continue;

                if (IsMemberMatching(m, bindingAttr) && m is FieldInfo) members.Add((FieldInfo)m);
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
            List<MemberInfo> members = new List<MemberInfo>();

            foreach (MemberInfo m in this.assembly.EnumerateMembers())
            {
                if (m.DeclaringType == null) continue;

                if (!String.Equals(m.DeclaringType.FullName, this.FullName, StringComparison.InvariantCulture)) continue;

                if (!String.Equals(m.Name, name, StringComparison.InvariantCulture)) continue;

                if (IsMemberMatching(m, bindingAttr)) members.Add(m);
            }

            return members.ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            List<MemberInfo> members = new List<MemberInfo>();

            foreach (MemberInfo m in this.assembly.EnumerateMembers())
            {
                if (m.DeclaringType == null) continue;

                if (!String.Equals(m.DeclaringType.FullName, this.FullName, StringComparison.InvariantCulture)) continue;

                if (IsMemberMatching(m, bindingAttr)) members.Add(m);
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
                string tn = assembly.MetadataReader.GetString(type.Namespace);
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

        public override Type MakeGenericType(params Type[] typeArguments)
        {
            return new ComplexType(this, ComplexTypeKind.GenInst, typeArguments);
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
