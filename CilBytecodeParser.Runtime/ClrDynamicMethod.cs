/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilBytecodeParser;
using CilBytecodeParser.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    class ClrDynamicMethod : CustomMethod
    {
        ClrObject method;
        ClrObject ilgen;
        DynamicMethodsAssembly owner;

        ulong[] GetDynamicTokenTable()
        {
            ClrObject resolver = method.GetObjectField("m_resolver");
            ClrObject scope = resolver.GetObjectField("m_scope");
            ClrObject dtokens = scope.GetObjectField("m_tokens");
            ClrObject items = dtokens.GetObjectField("_items");
            ClrType arrtype = items.Type;
            ulong addr = items.Address;
            int len = arrtype.GetArrayLength(addr);
            ulong[] ret = new ulong[len];

            for (int i = 0; i < len; i++)
            {
                object val = arrtype.GetArrayElementValue(addr, i);
                ret[i] = (ulong)val;                
            }

            return ret;
        }

        void ReadExceptionBlocks()
        {
            ClrObject exceptions;
            if (method.Type.Name.EndsWith("DynamicMethod"))
            {
                ClrObject resolver = method.GetObjectField("m_resolver");
                if (resolver != null && !resolver.IsNull) exceptions = resolver.GetObjectField("m_exceptions");
                else exceptions = new ClrObject();
            }
            else
            {
                exceptions = method.GetObjectField("m_exceptions");
            }

            if (exceptions.IsNull) return;

            int len = exceptions.Type.GetArrayLength(exceptions);

            for (int i = 0; i < len; i++)
            {
                ulong addr = exceptions.Type.GetArrayElementAddress(exceptions, i);
                ClrObject o = new ClrObject(addr, exceptions.Type.ComponentType);
                int startAddr = o.GetField<int>("m_startAddr");
                int m_endAddr = o.GetField<int>("m_endAddr");
            }

        }

        public ClrDynamicMethod(ClrObject m, DynamicMethodsAssembly ass)
        {
            this.method = m;
            ClrObject ilg = m.GetObjectField("m_ilGenerator");
            this.ilgen = ilg;
            this.owner = ass;
        }

        public override Type ReturnType
        {
            get { return UnknownType.Value; }
        }

        public override ITokenResolver TokenResolver
        {
            get { throw new NotImplementedException(); }
        }

        public override byte[] GetBytecode()
        {
            int size = ilgen.GetField<int>("m_length");
            ClrObject stream = ilgen.GetObjectField("m_ILStream");
            ClrType type = stream.Type;
            ulong obj = stream.Address;
            int len = type.GetArrayLength(obj);
            byte[] il = new byte[size];
            for (int i = 0; i < size; i++) il[i] = (byte)type.GetArrayElementValue(obj, i);
            return il;
        }

        public override int MaxStackSize
        {
            get { return 0; }
        }

        public override bool MaxStackSizeSpecified
        {
            get { return false; }
        }

        public override byte[] GetLocalVarSignature()
        {
            throw new NotImplementedException();
        }

        public override ExceptionBlock[] GetExceptionBlocks()
        {
            throw new NotImplementedException();
        }

        public override MethodAttributes Attributes
        {
            get 
            {
                MethodAttributes ret = (MethodAttributes)0;
                ret |= MethodAttributes.Public;
                ret |= MethodAttributes.Static;
                return ret;
            }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            return new ParameterInfo[] { };
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, 
            System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return this.owner.ChildType; }
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

        public override MemberTypes MemberType
        {
            get { return MemberTypes.Method; }
        }

        public override string Name
        {
            get 
            {
                string mname;
                try
                {                    
                    if (method.Type.Name.EndsWith("DynamicMethod"))
                    {
                        ClrObject rtdm = method.GetObjectField("m_dynMethod");
                        mname = rtdm.GetStringField("m_name");
                    }
                    else
                    {
                        mname = method.GetStringField("m_strName");
                    }
                }
                catch (ArgumentException)
                {
                    mname = "<UnknownDynamicMethod>";
                }
                return mname;
            }
        }

        public override Type ReflectedType
        {
            get { return UnknownType.Value; }
        }
    }
}
