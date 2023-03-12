/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
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
            else if (m is PropertyInfo || m is EventInfo)
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

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, 
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (types == null) throw new ArgumentNullException("types");

            ConstructorInfo[] members = this.GetConstructors(bindingAttr);

            for (int i = 0; i < members.Length; i++)
            {
                ConstructorInfo c = members[i];

                if (Utils.ParamsMatchSignature(c.GetParameters(), types))
                {
                    return c;
                }
            }

            return null; //not found
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            HashSet<ConstructorInfo> members = new HashSet<ConstructorInfo>(MemberComparer.Instance);
            MethodBase m;

            // Get constructors defined in this type
            foreach (MethodDefinitionHandle mdefh in this.type.GetMethods())
            {
                m = this.assembly.GetMethodDefinition(mdefh);

                if (m is ConstructorInfo && IsMemberMatching(m, bindingAttr)) members.Add((ConstructorInfo)m);
            }

            // Get constructors inherited from base type
            if (!bindingAttr.HasFlag(BindingFlags.DeclaredOnly) && this.BaseType != null)
            {
                ConstructorInfo[] inherited = this.BaseType.GetConstructors(bindingAttr);

                for (int i = 0; i < inherited.Length; i++)
                {
                    if (!Utils.IsInheritable(inherited[i])) continue;

                    members.Add(inherited[i]);
                }
            }

            return System.Linq.Enumerable.ToArray(members);
        }

        public override Type GetElementType()
        {
            return null;
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            EventInfo[] events = this.GetEvents(bindingAttr);

            for (int i = 0; i < events.Length; i++)
            {
                EventInfo m = events[i];

                if (string.Equals(m.Name, name, StringComparison.InvariantCulture)) return m;
            }

            return null;
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            List<EventInfo> members = new List<EventInfo>();
            EventInfo m;

            foreach (EventDefinitionHandle he in this.type.GetEvents())
            {
                EventDefinition e = this.assembly.MetadataReader.GetEventDefinition(he);
                m = new EventDef(e, he, this.assembly, this);
                if (IsMemberMatching(m, bindingAttr)) members.Add(m);
            }

            return members.ToArray();
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
            HashSet<FieldInfo> fields = new HashSet<FieldInfo>(MemberComparer.Instance);
            FieldInfo f;

            // Get fields defined in this type
            foreach (FieldDefinitionHandle hfield in this.type.GetFields())
            {
                FieldDefinition field = this.assembly.MetadataReader.GetFieldDefinition(hfield);
                f = new FieldDef(field, hfield, this.assembly, null);
                if (IsMemberMatching(f, bindingAttr)) fields.Add((FieldInfo)f);
            }

            // Get fields inherited from base type
            if (!bindingAttr.HasFlag(BindingFlags.DeclaredOnly) && this.BaseType != null)
            {
                FieldInfo[] inherited = this.BaseType.GetFields(bindingAttr);

                for (int i = 0; i < inherited.Length; i++)
                {
                    if (Utils.IsInheritable(inherited[i])) fields.Add(inherited[i]);
                }
            }

            return System.Linq.Enumerable.ToArray(fields);
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            InterfaceImplementationHandleCollection coll = this.type.GetInterfaceImplementations();
            List<Type> ret = new List<Type>(coll.Count);

            foreach (InterfaceImplementationHandle h in coll)
            {
                InterfaceImplementation ii = this.assembly.MetadataReader.GetInterfaceImplementation(h);
                ret.Add(this.assembly.GetTypeByHandle(ii.Interface));
            }

            return ret.ToArray();
        }

        static bool ContainsMethod(List<MethodInfo> coll, MethodInfo match)
        {
            for (int i = 0; i < coll.Count; i++)
            {
                if (coll[i].MetadataToken == match.MetadataToken)
                {
                    //all methods are in the same module, so only need to compare
                    //by metadata token
                    return true;
                }
            }

            return false;
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("Type is not an interface");
            }

            MethodImplementationHandleCollection coll = this.type.GetMethodImplementations();
            List<MethodInfo> bodys = new List<MethodInfo>();
            List<MethodInfo> decls = new List<MethodInfo>();

            //explicitly implemented
            foreach (MethodImplementationHandle h in coll)
            {
                if (h.IsNil) continue;

                MethodImplementation impl = this.assembly.MetadataReader.GetMethodImplementation(h);
                MethodInfo decl = this.assembly.GetMethodByHandle(impl.MethodDeclaration);
                Debug.Assert(decl != null);

                if (!Utils.TypeEquals(decl.DeclaringType, interfaceType)) continue;

                MethodInfo body = this.assembly.GetMethodByHandle(impl.MethodBody);
                Debug.Assert(body != null);
                bodys.Add(body);
                decls.Add(decl);
            }

            //implicitly implemented
            MethodInfo[] ifMethods = interfaceType.GetMethods();

            for (int i = 0; i < ifMethods.Length; i++)
            {
                //interface method
                MethodInfo ifMethod = ifMethods[i];

                //check if already picked up
                if (ContainsMethod(decls, ifMethod)) continue;

                //find method with the same name and signature on current type
                Type[] sig = Utils.GetParameterTypesArray(ifMethod);
                MethodInfo mi = this.GetMethod(ifMethod.Name, Utils.AllMembers(), null, sig, null);

                if (mi != null)
                {
                    bodys.Add(mi);
                    decls.Add(ifMethod);
                }
            }

            if (decls.Count == 0)
            {
                throw new ArgumentException("Type does not implement the specified interface");
            }

            Debug.Assert(decls.Count == bodys.Count);

            InterfaceMapping map = new InterfaceMapping();
            map.InterfaceMethods = decls.ToArray();
            map.TargetMethods = bodys.ToArray();
            map.InterfaceType = interfaceType;
            map.TargetType = this;
            return map;
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
            HashSet<MemberInfo> members = new HashSet<MemberInfo>(MemberComparer.Instance);
            
            // Get members defined in this type
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

            //events
            EventInfo[] events = this.GetEvents(bindingAttr);

            for (int i = 0; i < events.Length; i++)
            {
                members.Add(events[i]);
            }

            // Get members inherited from base type
            if (!bindingAttr.HasFlag(BindingFlags.DeclaredOnly) && this.BaseType != null)
            {
                MemberInfo[] inherited = this.BaseType.GetMembers(bindingAttr);

                for (int i = 0; i < inherited.Length; i++)
                {
                    if (!Utils.IsInheritable(inherited[i])) continue;

                    members.Add(inherited[i]);
                }
            }

            return System.Linq.Enumerable.ToArray(members);
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null) throw new ArgumentNullException("name");

            MethodInfo[] members = this.GetMethods(bindingAttr);
            List<MethodInfo> matching = new List<MethodInfo>();

            for (int i = 0; i < members.Length; i++)
            {
                MethodInfo m = members[i];

                if (callConvention != CallingConventions.Any && m.CallingConvention != callConvention) continue;
                if (!Utils.StrEquals(m.Name, name)) continue;

                if (types != null && Utils.ParamsMatchSignature(m.GetParameters(), types))
                {
                    return m; //exact match by name and signature
                }
                else if (types == null) 
                {
                    matching.Add(m); //adding to candidates for name match
                }
            }

            if (matching.Count == 1)
            {
                //if we found exactly one with the specified name, we could return it
                return matching[0];
            }
            else if (matching.Count == 0)
            {
                return null; //not found anything
            }
            else throw new AmbiguousMatchException("More than one method with the specified name exist");
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            HashSet<MethodInfo> members = new HashSet<MethodInfo>(MemberComparer.Instance);
            MethodBase m;

            // Get methods defined in this type
            foreach (MethodDefinitionHandle mdefh in this.type.GetMethods())
            {
                m = this.assembly.GetMethodDefinition(mdefh);

                if (m is MethodInfo && IsMemberMatching(m, bindingAttr)) members.Add((MethodInfo)m);
            }

            // Get methods inherited from base type
            if (!bindingAttr.HasFlag(BindingFlags.DeclaredOnly) && this.BaseType != null)
            {
                MethodInfo[] inherited = this.BaseType.GetMethods(bindingAttr);

                for (int i = 0; i < inherited.Length; i++)
                {
                    if (!Utils.IsInheritable(inherited[i])) continue;

                    members.Add(inherited[i]);
                }
            }

            return System.Linq.Enumerable.ToArray(members);
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

            if (bindingAttr == BindingFlags.Default) return new PropertyInfo[0];
            else if (bindingAttr.HasFlag(BindingFlags.DeclaredOnly)) return this.properties;

            HashSet<PropertyInfo> props = new HashSet<PropertyInfo>(MemberComparer.Instance);

            // Properties defined in this type
            for (int i = 0; i < this.properties.Length; i++)
            {
                props.Add(this.properties[i]);
            }

            // Inherited properties
            if (this.BaseType != null)
            {
                PropertyInfo[] inherited = this.BaseType.GetProperties(bindingAttr);

                for (int i = 0; i < inherited.Length; i++)
                {
                    if (Utils.IsInheritable(inherited[i])) props.Add(inherited[i]);
                }
            }

            return System.Linq.Enumerable.ToArray(props);
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            PropertyInfo[] props = this.GetProperties(bindingAttr);

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                if (Utils.StrEquals(p.Name, name)) return p;
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
            get 
            {
                //workaround Type.IsSerializable NullReferenceException in .NET Core 3.1
                return this; 
            }
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
            
            return Utils.ReadCustomAttributes(coll, this, this.assembly);
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
            return Utils.GetGenericParameters(this.assembly, this, hcoll);
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

        public override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                LayoutKind lk = 0;

                switch (type.Attributes & TypeAttributes.LayoutMask)
                {
                    case TypeAttributes.AutoLayout: lk = LayoutKind.Auto; break;
                    case TypeAttributes.SequentialLayout: lk = LayoutKind.Sequential; break;
                    case TypeAttributes.ExplicitLayout: lk = LayoutKind.Explicit; break;
                }

                var ret = new System.Runtime.InteropServices.StructLayoutAttribute(lk);

                switch (type.Attributes & TypeAttributes.StringFormatMask)
                {
                    case TypeAttributes.UnicodeClass: ret.CharSet = CharSet.Unicode; break;
                    case TypeAttributes.AnsiClass: ret.CharSet = CharSet.Ansi; break;
                    default: ret.CharSet = CharSet.Auto; break;
                }
                
                TypeLayout tl = this.type.GetLayout();
                ret.Pack = tl.PackingSize;
                ret.Size = tl.Size;
                return ret;
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
