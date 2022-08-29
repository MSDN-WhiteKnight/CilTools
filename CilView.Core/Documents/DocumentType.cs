/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using CilView.Core.Syntax;

namespace CilView.Core.Documents
{
    /// <summary>
    /// Synthesized type that represents the type definition syntax in source document
    /// </summary>
    public class DocumentType : Type
    {
        DocumentAssembly _owner;
        DocumentSyntax _syntax;
        string _fullname;

        public DocumentType(DocumentAssembly owner, DocumentSyntax syntax, string name)
        {
            this._owner = owner;
            this._syntax = syntax;
            this._fullname = name;
        }

        public DocumentSyntax Syntax
        {
            get { return this._syntax; }
        }

        public string GetDocumentText()
        {
            return this._syntax.ToString();
        }

        public override Assembly Assembly => this._owner;

        public override string AssemblyQualifiedName
        {
            get { return this._fullname + ", " + this._owner.FullName; }
        }

        public override Type BaseType => typeof(object);

        public override string FullName => this._fullname;

        public override Guid GUID => new Guid();

        public override Module Module => throw new NotImplementedException();

        public override string Namespace
        {
            get
            {
                string tn = this._fullname;
                int index = tn.LastIndexOf('.');

                if (index < 0) return "";
                else return tn.Substring(0, index);
            }
        }

        public override Type UnderlyingSystemType => throw new NotImplementedException();

        public override string Name
        {
            get
            {
                string tn = this._fullname;
                int index = tn.LastIndexOf('.');

                if (index < 0) return tn;
                if (index + 1 >= tn.Length) return tn;
                else return tn.Substring(index + 1);
            }
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return new ConstructorInfo[0];
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override Type GetElementType()
        {
            throw new NotImplementedException();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return new EventInfo[0];
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
            return null;
        }

        public override Type[] GetInterfaces()
        {
            return new Type[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return new MemberInfo[0];
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return new MethodInfo[0];
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return new Type[0];
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return new PropertyInfo[0];
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, 
            object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new InvalidOperationException("Cannot invoke members on type loaded into reflection-only context");
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return default;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, 
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, 
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return null;
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, 
            Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            return null;
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
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
    }
}
