/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection.Methods
{
    internal class MethodInfoWrapper : MethodInfo, ICustomMethod
    {
        MethodInfo srcmethod;
        MethodBodyData mbd;
        
        public MethodInfoWrapper(MethodInfo mi) : base()
        {
            this.srcmethod = mi;
            this.mbd = new MethodBodyData(mi);
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

        public override Type ReturnType
        {
            get
            {
                return srcmethod.ReturnType;
            }
        }

        public ITokenResolver TokenResolver
        {
            get
            {
                return this.mbd.TokenResolver;
            }
        }

        public byte[] GetBytecode()
        {
            return this.mbd.GetBytecode();
        }

        public byte[] GetLocalVarSignature()
        {
            return this.mbd.GetLocalVarSignature();
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
            get { return this.mbd.MaxStackSize; }
        }

        public bool MaxStackSizeSpecified
        {
            get { return this.mbd.MaxStackSizeSpecified; }
        }

        public ExceptionBlock[] GetExceptionBlocks()
        {
            return this.mbd.GetExceptionBlocks();
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this.srcmethod.GetBaseDefinition();
        }

        public bool InitLocals
        {
            get { return this.mbd.InitLocals; }
        }

        public bool InitLocalsSpecified
        {
            get { return this.mbd.InitLocalsSpecified; }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return this.srcmethod.ReturnTypeCustomAttributes; }
        }
        
        public virtual MethodBase GetDefinition()
        {
            return null;
        }

        public virtual PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        public override int MetadataToken => this.srcmethod.MetadataToken;
    }
}
