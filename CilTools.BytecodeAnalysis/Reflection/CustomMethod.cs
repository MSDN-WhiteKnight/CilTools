/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    /// <summary>
    /// A base class for MethodBase implementations providing custom mechanisms for extracting bytecode data. 
    /// Inherit from this class when you want CilTools.BytecodeAnalysis to process bytecode from your custom data source, instead of 
    /// reflection.
    /// </summary>
    public abstract class CustomMethod : MethodBase
    {
        /// <summary>
        /// When overridden in the derived class, returns the type of this method's return value
        /// </summary>
        /// <remarks>Return null if the return type is not applicable (such as for constructors).</remarks>
        public abstract Type ReturnType { get; }

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

        public virtual MethodBase GetDefinition()
        {
            return null;
        }

        public virtual PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        /// <summary>
        /// Converts MethodBase into the form suitable for processing by CilTools.BytecodeAnalysis
        /// </summary>        
        internal static CustomMethod PrepareMethod(MethodBase src)
        {
            if (src == null) return null;

            if (src is CustomMethod) return (CustomMethod)src;
            else return new MethodBaseWrapper(src);
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
