/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Provides a base class for <see cref="MethodInfo"/> subclasses that implement <see cref="ICustomMethod"/>.
    /// </summary>
    public abstract class CustomMethod : MethodInfo, ICustomMethod
    {
        /// <summary>
        /// Gets the return type of this method.
        /// </summary>
        public override Type ReturnType { get; }

        /// <summary>
        /// When overridden in the derived class, returns an object that can be used to convert metadata tokens 
        /// into corresponding reflection objects
        /// </summary>
        public abstract ITokenResolver TokenResolver { get; }

        /// <summary>
        /// When overridden in the derived class, returns the CIL bytecode of the method body
        /// </summary>
        /// <returns>CIL bytecode as byte array</returns>
        public abstract byte[] GetBytecode();

        /// <summary>
        /// When overridden in the derived class, returns the maximum size of operand stack during method execution
        /// </summary>
        public abstract int MaxStackSize { get; }

        /// <summary>
        /// When overridden in the derived class, specifies whether the MaxStackSize property value is defined
        /// </summary>
        public abstract bool MaxStackSizeSpecified { get; }

        /// <summary>
        /// When overridden in the derived class, specifies whether the local variables are initialized
        /// </summary>
        public abstract bool InitLocals { get; }

        /// <summary>
        /// When overridden in the derived class, specifies whether the <c>InitLocals</c> property value is defined
        /// </summary>
        public abstract bool InitLocalsSpecified { get; }
        
        /// <summary>
        /// When overridden in the derived class, returns the local variable signature as an array of bytes
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetLocalVarSignature();

        /// <summary>
        /// When overridden in the derived class, returns the list of exception handling blocks in the method's body
        /// </summary>        
        public abstract ExceptionBlock[] GetExceptionBlocks();

        /// <summary>
        /// Returns the array of local variable declarations of this method
        /// </summary>
        /// <remarks>
        /// The default implementation reads local varaibles from the signature returned by GetLocalVarSignature method
        /// </remarks>
        public virtual LocalVariable[] GetLocalVariables()
        {
            byte[] sig = this.GetLocalVarSignature();

            return CilTools.Reflection.LocalVariable.ReadSignature(sig, this.TokenResolver,this);
        }

        /// <summary>
        /// When overridden in the derived class, gets the method definition for the generic method. 
        /// Returns null if this instance does not represent the generic method.
        /// </summary>
        /// <remarks>
        /// The default implementation always returns null
        /// </remarks>
        public virtual MethodBase GetDefinition()
        {
            return null;
        }

        /// <summary>
        /// When overridden in the derived class, gets P/Invoke parameters for the imported unmanaged 
        /// method. Returns null if this instance does not represent an imported unmanaged method.
        /// </summary>
        /// <remarks>
        /// The default implementation always returns null
        /// </remarks>
        public virtual PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        /// <inheritdoc/>
        public override MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException("GetBaseDefinition should be implemented in derived class");
        }

        /// <inheritdoc/>
        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                throw new NotImplementedException("ReturnTypeCustomAttributes should be implemented in derived class");
            }
        }

        /// <summary>
        /// Converts <see cref="MethodBase"/> into the form suitable for processing by CilTools.BytecodeAnalysis
        /// </summary>
        internal static ICustomMethod PrepareMethod(MethodBase src)
        {
            if (src == null) return null;

            ICustomMethod ret;

            if (src is ICustomMethod) ret = (ICustomMethod)src;
            else if (src is MethodInfo) ret = new MethodInfoWrapper((MethodInfo)src);
            else if (src is ConstructorInfo) ret = new ConstructorInfoWrapper((ConstructorInfo)src);
            else ret = new MethodBaseWrapper(src);

            Debug.Assert(ret is ICustomMethod && ret is MethodBase,
                "PrepareMethod should return type that inherits from MethodBase and implements ICustomMethod");

            return ret;
        }

        /// <summary>
        /// Creates an object that can be used to resolve tokens in the context of specified method
        /// </summary>
        internal static ITokenResolver CreateResolver(MethodBase mb)
        {
            if (Types.IsDynamicMethod(mb)) return new ModuleWrapperDynamic(mb);
            else return new ModuleWrapper(mb);
        }
    }
}
