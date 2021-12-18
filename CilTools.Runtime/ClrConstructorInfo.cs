/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Reflection;
using Microsoft.Diagnostics.Runtime;
using CilTools.Reflection;
using CilTools.Runtime.Methods;

namespace CilTools.Runtime
{
    /// <summary>
    /// Represents information about the constructor in an external CLR instance
    /// </summary>
    public class ClrConstructorInfo : ConstructorInfo, ICustomMethod
    {
        ClrMethodData md;

        internal ClrConstructorInfo(ClrMethod m, ClrTypeInfo owner)
        {
            this.md = new ClrMethodData(m, owner);
        }

        /// <summary>
        /// Gets the underlying ClrMD method object
        /// </summary>
        public ClrMethod InnerMethod { get { return this.md.InnerMethod; } }

        /// <inheritdoc/>
        public Type ReturnType
        {
            get { return null; }
        }

        /// <inheritdoc/>
        public ITokenResolver TokenResolver
        {
            get { return this.md.Assembly; }
        }
        
        /// <inheritdoc/>
        public byte[] GetBytecode()
        {
            return this.md.GetBytecode();
        }

        /// <inheritdoc/>
        public int MaxStackSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public bool MaxStackSizeSpecified
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public byte[] GetLocalVarSignature()
        {
            return new byte[] { }; //not implemented
        }

        /// <inheritdoc/>
        public ExceptionBlock[] GetExceptionBlocks()
        {
            return this.md.GetExceptionBlocks();
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get { return this.md.GetAttributes(); }
        }

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            return new ParameterInfo[] { };
        }

        /// <inheritdoc/>
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters,
            CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        /// <inheritdoc/>
        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        /// <inheritdoc/>
        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override Type DeclaringType
        {
            get { return this.md.OwnerType; }
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new object[] { };
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[] { };
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }
        
        /// <inheritdoc/>
        public LocalVariable[] GetLocalVariables()
        {
            return new LocalVariable[0];
        }

        /// <inheritdoc/>
        public MethodBase GetDefinition()
        {
            return null;
        }

        /// <inheritdoc/>
        public PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        /// <inheritdoc/>
        public override MemberTypes MemberType
        {
            get { return MemberTypes.Constructor; }
        }

        /// <inheritdoc/>
        public override string Name
        {
            get { return this.md.InnerMethod.Name; }
        }

        /// <inheritdoc/>
        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                return (int)this.md.InnerMethod.MetadataToken;
            }
        }

        /// <inheritdoc/>
        public bool InitLocals
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public bool InitLocalsSpecified
        {
            get { return false; }
        }
    }
}
