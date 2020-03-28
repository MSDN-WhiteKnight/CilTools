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
    class ClrMethodInfo : CustomMethod
    {
        ClrMethod method;
        ClrAssemblyInfo assembly;
        DataTarget target;
        ClrTypeInfo type;

        public ClrMethodInfo(ClrMethod m, ClrTypeInfo owner)
        {
            this.method = m;
            this.assembly = (ClrAssemblyInfo)owner.Assembly;
            this.type = owner;

            if (assembly != null) this.target = assembly.InnerModule.Runtime.DataTarget;
        }

        public ClrMethod InnerMethod { get { return this.method; } }

        public override Type ReturnType
        {
            get 
            {
                if (method.IsConstructor || method.IsClassConstructor) return null;
                else return UnknownType.Value;
            }
        }

        public override ITokenResolver TokenResolver
        {
            get { return this.assembly; }
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
            get
            {
                MethodAttributes ret = (MethodAttributes)0;
                if (method.IsAbstract) ret |= MethodAttributes.Abstract;
                if (method.IsFinal) ret |= MethodAttributes.Final;
                if (method.IsInternal) ret |= MethodAttributes.Assembly;
                if (method.IsPrivate) ret |= MethodAttributes.Private;
                if (method.IsProtected) ret |= MethodAttributes.Family;
                if (method.IsPublic) ret |= MethodAttributes.Public;
                if (method.IsStatic) ret |= MethodAttributes.Static;
                if (method.IsVirtual) ret |= MethodAttributes.Virtual;
                if (method.IsPInvoke) ret |= MethodAttributes.PinvokeImpl;
                if (method.IsSpecialName) ret |= MethodAttributes.SpecialName;
                if (method.IsRTSpecialName) ret |= MethodAttributes.RTSpecialName;
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
            get { return this.type; }
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

        public override int MetadataToken
        {
            get
            {
                return (int)method.MetadataToken;
            }
        }
    }
}
