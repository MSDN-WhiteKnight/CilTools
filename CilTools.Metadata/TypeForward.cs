/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace CilTools.Metadata
{
    class TypeForward : Type
    {
        ExportedType et;
        MetadataAssembly assembly;
        
        internal TypeForward(ExportedType t, MetadataAssembly ass)
        {
            Debug.Assert(ass != null, "ass in TypeForward() should not be null");

            this.et = t;
            this.assembly = ass;
        }
        
        public override Assembly Assembly
        {
            get
            {
                EntityHandle eh = this.et.Implementation;

                if (eh.IsNil) return MetadataAssembly.UnknownAssembly;

                if (eh.Kind == HandleKind.AssemblyReference)
                {
                    AssemblyReference ar = assembly.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)eh);
                    return new AssemblyRef(ar, (AssemblyReferenceHandle)eh, this.assembly);
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
                return UnknownType.Value;
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

                if (this.DeclaringType != null)
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
            return this.et.Attributes;
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
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
                if (this.et.Namespace.IsNil) return string.Empty;

                return assembly.MetadataReader.GetString(this.et.Namespace);
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
                return assembly.MetadataReader.GetString(this.et.Name);
            }
        }

        public override int MetadataToken
        {
            get { return 0; }
        }
        
        public override Type DeclaringType
        {
            get { return null; }
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
