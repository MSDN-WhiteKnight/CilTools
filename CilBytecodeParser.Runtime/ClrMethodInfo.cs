/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Diagnostics.Runtime;
using CilBytecodeParser;
using CilBytecodeParser.Reflection;

namespace CilBytecodeParser.Runtime
{
    internal class ClrMethodInfo : CustomMethod
    {
        ClrMethod method;
        DataTarget target;        

        internal ClrMethodInfo(ClrMethod m, DataTarget dt)
        {
            this.method = m;
            this.target = dt;
        }

        public override Type ReturnType
        {
            get { return new UnknownType(); }
        }

        public override ITokenResolver TokenResolver
        {
            get { throw new NotImplementedException(); }
        }

        public override byte[] GetBytecode()
        {
            byte[] il;
            int bytesread;
            ILInfo ildata = method.IL;

            if (ildata == null)
            {
                throw new CilParserException("Cannot read IL of the method "+method.Name);
            }
            else
            {
                il = new byte[ildata.Length];
                target.ReadProcessMemory(ildata.Address, il, ildata.Length, out bytesread);
                return il;
            }
        }

        public override int MaxStackSize
        {
            get
            {
                ILInfo ildata = method.IL;

                if (ildata == null)
                {
                    throw new CilParserException("Cannot read IL of the method " + method.Name);
                }
                else
                {
                    return ildata.MaxStack;
                }
            }
        }

        public override bool MaxStackSizeSpecified
        {
            get
            {
                ILInfo ildata = method.IL;

                if (ildata == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
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
            get { return (MethodAttributes)0; }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            return new ParameterInfo[] { };
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return new UnknownType(); }
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
            get { return method.Name; }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }
    }
}
