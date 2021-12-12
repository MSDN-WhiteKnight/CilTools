/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;

namespace CilTools.Reflection
{
    /// <summary>
    /// Defines API providing custom information about methods. Implement this interface on your custom class inheriting from 
    /// <see cref="MethodBase"/> if you want CilTools.BytecodeAnalysis library to process bytecode from your custom data source, 
    /// instead of the standard reflection.
    /// </summary>
    public interface ICustomMethod
    {
        /// <summary>
        /// Gets the type of this method's return value
        /// </summary>
        /// <remarks>
        /// Returns null if the return type is not applicable (such as for constructors).
        /// </remarks>
        Type ReturnType { get; }

        /// <summary>
        /// Gets an object that can be used to convert metadata tokens into corresponding reflection objects
        /// </summary>
        ITokenResolver TokenResolver { get; }

        /// <summary>
        /// Gets the CIL bytecode of the method body
        /// </summary>
        /// <returns>CIL bytecode as byte array</returns>
        byte[] GetBytecode();

        /// <summary>
        /// Gets the maximum size of operand stack during method execution
        /// </summary>
        int MaxStackSize { get; }

        /// <summary>
        /// Gets the value specifying whether the <see cref="MaxStackSize"/> property value is defined
        /// </summary>
        bool MaxStackSizeSpecified { get; }

        /// <summary>
        /// Gets the value specifying whether the local variables are initialized
        /// </summary>
        bool InitLocals { get; }

        /// <summary>
        /// Gets the value specifying whether the <see cref="InitLocals"/> property value is defined
        /// </summary>
        bool InitLocalsSpecified { get; }

        /// <summary>
        /// Gets the local variable signature as an array of bytes
        /// </summary>
        /// <returns></returns>
        byte[] GetLocalVarSignature();

        /// <summary>
        /// Gets the list of exception handling blocks in the method's body
        /// </summary>        
        ExceptionBlock[] GetExceptionBlocks();

        /// <summary>
        /// Gets the array of local variable declarations of this method
        /// </summary>        
        LocalVariable[] GetLocalVariables();

        /// <summary>
        /// Gets the method definition for the generic method. 
        /// Returns null if this instance does not represent the generic method.
        /// </summary>        
        MethodBase GetDefinition();

        /// <summary>
        /// Gets P/Invoke parameters for the imported unmanaged method. 
        /// Returns null if this instance does not represent an imported unmanaged method.
        /// </summary>
        PInvokeParams GetPInvokeParams();
    }
}
