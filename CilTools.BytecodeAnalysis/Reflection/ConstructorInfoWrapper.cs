﻿/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    internal class ConstructorInfoWrapper: ConstructorInfo, ICustomMethod
    {
        ConstructorInfo srcmethod;
        ITokenResolver resolver;
        int _stacksize;
        bool _hasstacksize;
        byte[] _code;
        byte[] _localssig;
        ExceptionBlock[] _blocks;
        bool _localsinit;
        bool _haslocalsinit;

        public ConstructorInfoWrapper(ConstructorInfo ci) : base()
        {
            this.srcmethod = ci;
            this.resolver = CustomMethod.CreateResolver(ci);

            MethodBody body = srcmethod.GetMethodBody();

            if (body != null)
            {
                this._code = body.GetILAsByteArray();
                int token = body.LocalSignatureMetadataToken;

                if (token == 0) this._localssig = new byte[0];
                else this._localssig = this.resolver.ResolveSignature(token);

                this._stacksize = body.MaxStackSize;
                this._hasstacksize = true;

                this._blocks = new ExceptionBlock[body.ExceptionHandlingClauses.Count];

                for (int i = 0; i < body.ExceptionHandlingClauses.Count; i++)
                {
                    this._blocks[i] = ExceptionBlock.FromReflection(body.ExceptionHandlingClauses[i]);
                }

                this._localsinit = body.InitLocals;
                this._haslocalsinit = true;
            }
            else
            {
                this._code = new byte[0];
                this._localssig = new byte[0];
                this._blocks = new ExceptionBlock[0];
                this._hasstacksize = false;
                this._haslocalsinit = false;
            }
        }

        public override MethodAttributes Attributes { get { return srcmethod.Attributes; } }

        public override RuntimeMethodHandle MethodHandle { get { return srcmethod.MethodHandle; } }

        public override Type DeclaringType { get { return srcmethod.DeclaringType; } }

        public override MemberTypes MemberType { get { return srcmethod.MemberType; } }

        public override string Name { get { return srcmethod.Name; } }

        public override Type ReflectedType { get { return srcmethod.ReflectedType; } }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return srcmethod.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return srcmethod.GetCustomAttributes(attributeType, inherit);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return srcmethod.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return srcmethod.GetParameters();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            return srcmethod.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            return srcmethod.Invoke(invokeAttr, binder, parameters, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return srcmethod.IsDefined(attributeType, inherit);
        }

        public override bool IsGenericMethod
        {
            get
            {
                return srcmethod.IsGenericMethod;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return srcmethod.GetGenericArguments();
        }

        public Type ReturnType
        {
            get { return null; }
        }

        public ITokenResolver TokenResolver
        {
            get
            {
                return this.resolver;
            }
        }

        public byte[] GetBytecode()
        {
            return this._code;
        }

        public byte[] GetLocalVarSignature()
        {
            return this._localssig;
        }

        LocalVariable[] GetLocalVariables_Default()
        {
            byte[] sig = this.GetLocalVarSignature();

            return LocalVariable.ReadSignature(sig, this.TokenResolver, this);
        }

        public LocalVariable[] GetLocalVariables()
        {
            LocalVariable[] ret = null;

            try
            {
                ret = this.GetLocalVariables_Default();
            }
            catch (NotSupportedException) { }
            catch (ArgumentOutOfRangeException) { }

            if (ret != null) return ret;
            else return LocalVariable.FromReflection(this.srcmethod);
        }

        public int MaxStackSize
        {
            get { return this._stacksize; }
        }

        public bool MaxStackSizeSpecified
        {
            get { return this._hasstacksize; }
        }

        public ExceptionBlock[] GetExceptionBlocks()
        {
            return this._blocks;
        }

        public bool InitLocals
        {
            get { return this._localsinit; }
        }

        public bool InitLocalsSpecified
        {
            get { return this._haslocalsinit; }
        }

        public virtual MethodBase GetDefinition()
        {
            return null;
        }

        public virtual PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        
    }
}