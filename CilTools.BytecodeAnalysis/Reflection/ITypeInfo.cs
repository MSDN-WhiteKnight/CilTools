/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents type information not covered by standard reflection.
    /// </summary>
    /// <remarks>
    /// <para>This interface exposes the information applicable to types in signatures (such as a method signature), 
    /// not to type definitions. For example, it enables you to programmatically inspect function 
    /// pointer types, which is currently (as of .NET 5) not supported by the standard reflection implementation.</para>
    /// <para>Some APIs in <c>CilTools.Metadata</c>, such as <c>ParameterInfo.ParameterType</c> 
    /// from methods loaded using this library, 
    /// could return <see cref="Type"/> instances that implements this interface. Cast them to the interface 
    /// using <c>is</c>/<c>as</c> C# operators and use properties to get the information you need. </para>
    /// </remarks>
    public interface ITypeInfo
    {
        /// <summary>
        /// Enumerates custom modifiers associated with the type
        /// </summary>
        IEnumerable<CustomModifier> Modifiers { get; }

        /// <summary>
        /// Gets a value indicating whether the type represents a function pointer
        /// </summary>
        bool IsFunctionPointer { get; }

        /// <summary>
        /// Gets the target function signature, if the type represents a function pointer. Otherwise, returns <c>null</c>.
        /// </summary>
        Signature TargetSignature { get; }
    }
}
