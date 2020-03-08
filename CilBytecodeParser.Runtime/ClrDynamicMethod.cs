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
        ClrTokenTable tokentable;
        //DynamicResolver.m_scope.m_tokens (ArrayList)
        //ArrayList: .field private object[] _items

        public ClrDynamicMethod(ClrObject m, ClrTokenTable tokens)
        {
            this.method = m;
            ClrObject ilg = m.GetObjectField("m_ilGenerator");
            this.ilgen = ilg;
            this.tokentable = tokens;
        }

        public override Type ReturnType
        {
            get { return UnknownType.Value; }
        }

        public override ITokenResolver TokenResolver
        {
            get { return this.tokentable; }
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
            throw new NotImplementedException();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return UnknownType.Value; }
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
