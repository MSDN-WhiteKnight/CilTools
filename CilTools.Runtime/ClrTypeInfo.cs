/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    class ClrTypeInfo : Type
    {
        ClrType type;
        Type basetype;
        Type elemType;
        ClrAssemblyInfo assembly;

        internal static Type LoadTypeInfo(ClrTypeInfo ownertype, ClrType ft)
        {
            ClrAssemblyInfo ownerass = (ClrAssemblyInfo)ownertype.Assembly;
            Type t=null;

            if (ownerass.AssemblyReader == null) return UnknownType.Value;
            if (ft.Module == null) return UnknownType.Value;
            if (ownertype.InnerType.Module == null) return UnknownType.Value;

            //determine type's containing assembly
            ClrAssemblyInfo ftAss;
            if (ft.Module.AssemblyId == ownertype.InnerType.Module.AssemblyId)
            {
                //same assembly
                ftAss = ownerass;
            }
            else
            {
                //try preloaded assemblies
                //this is an optimization to avoid calling the expensive AssemblyReader.Read
                Assembly ass = ownerass.AssemblyReader.GetResolver(ft.Module) as Assembly;

                if (ass != null) 
                {
                    t = ass.GetType(ft.Name);
                }

                if (t != null) return t;

                //read containing assembly
                ftAss = ownerass.AssemblyReader.Read(ft.Module);
            }

            if (ftAss == null) //unable to find existing type instance, create new one
            {
                return new ClrTypeInfo(ft, ClrAssemblyInfo.UnknownAssembly);
            }

            //load type from assembly
            t = ftAss.GetType(ft.Name);
            return t;
        }

        public ClrTypeInfo(ClrType t, ClrAssemblyInfo ass)
        {
            Debug.Assert(t != null, "t in ClrTypeInfo() should not be null");
            Debug.Assert(ass != null, "ass in ClrTypeInfo() should not be null");

            this.type = t;
            this.assembly = ass;
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
                if (type.BaseType == null) return null;

                if (this.basetype == null)
                {
                    this.basetype = LoadTypeInfo(this, type.BaseType);
                }

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
            if (this.elemType == null)
            {
                if ((type.IsArray || type.IsPointer) && type.ComponentType != null)
                {
                    this.elemType = LoadTypeInfo(this, type.ComponentType);
                }
            }

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
            FieldInfo[] fields = this.GetFields(bindingAttr);

            for (int i = 0; i < fields.Length; i++) 
            {
                FieldInfo fi = fields[i];
                if (StrEquals(fi.Name, name)) return fi;
            }

            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            int cap = this.type.Fields.Count + this.type.Fields.Count + this.type.ThreadStaticFields.Count;
            List<ClrField> clrFields = new List<ClrField>(cap);
            List<FieldInfo> fields = new List<FieldInfo>();
            FieldInfo fi;
            bool access_match;
            bool sem_match;

            foreach (var f in this.type.Fields)
            {
                clrFields.Add(f);
            }

            foreach (var f in this.type.StaticFields)
            {
                clrFields.Add(f);
            }

            foreach (var f in this.type.ThreadStaticFields)
            {
                clrFields.Add(f);
            }

            for (int i = 0; i < clrFields.Count; i++) 
            {
                int token = (int)clrFields[i].Token;
                fi = this.assembly.ResolveField(token);

                if (fi == null)
                {
                    fi = new ClrFieldInfo(clrFields[i], this);
                    this.assembly.SetMemberByToken(token, fi);
                }

                access_match = false;
                sem_match = false;

                if (bindingAttr.HasFlag(BindingFlags.Public) && fi.IsPublic) access_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.NonPublic) && !fi.IsPublic) access_match = true;

                if (bindingAttr.HasFlag(BindingFlags.Static) && fi.IsStatic) sem_match = true;
                else if (bindingAttr.HasFlag(BindingFlags.Instance) && !fi.IsStatic) sem_match = true;

                if (access_match && sem_match) fields.Add(fi);
            }

            return fields.ToArray();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            throw new NotImplementedException();
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

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            List<MemberInfo> ret = new List<MemberInfo>();

            MemberInfo[] members = this.GetMembers(bindingAttr);

            for (int i = 0; i < members.Length; i++)
            {
                MemberInfo mi = members[i];
                if (StrEquals(mi.Name, name)) ret.Add(mi);
            }

            return ret.ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            List<MemberInfo> members = new List<MemberInfo>();

            FieldInfo[] fields = this.GetFields(bindingAttr);

            for (int i = 0; i < fields.Length; i++) 
            {
                members.Add(fields[i]);
            }

            foreach (ClrMethod m in this.type.Methods)
            {
                if (m.Type != null)
                {
                    if (!StrEquals(m.Type.Name,this.type.Name)) continue; //skip inherited methods
                }

                int token = (int)m.MetadataToken;
                MethodBase mb = this.assembly.ResolveMethod(token);

                if (mb == null) 
                {
                    mb = ClrMethodInfo.CreateMethod(m, this);
                    this.assembly.SetMemberByToken((int)m.MetadataToken, mb);
                }

                if (IsMemberMatching(mb, bindingAttr)) members.Add(mb);
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
                string tn;
                tn = type.Name;
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

        internal static bool StrEquals(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCulture);
        }

        internal static bool TypeEquals(Type left, Type right)
        {
            if (Type.ReferenceEquals(left, right)) return true;

            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if (right == null) return false;

            string left_assname = String.Empty;
            string right_assname = String.Empty;

            if (left.Assembly != null) left_assname = left.Assembly.GetName().Name;
            if (right.Assembly != null) right_assname = right.Assembly.GetName().Name;

            return StrEquals(left_assname, right_assname) && StrEquals(left.FullName, right.FullName);
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

        public override int GetHashCode()
        {
            if (this.type == null) return 0;
            if (this.type.Name == null) return 0;

            return this.type.Name.GetHashCode();
        }

        public override string ToString()
        {
            return type.Name;
        }
              
    }
}
