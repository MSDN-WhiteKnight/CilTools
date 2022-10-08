/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents an object that can provide information about method parameters
    /// </summary>
    /// <remarks>
    /// External methods in .NET assemblies are referenced using a signature that contains information about their 
    /// parameter types, but does not contain other information, such as parameter names. Getting that information
    /// requires resolving a reference and loading the actual implementation pointed by it, which can fail if the 
    /// containing assembly is not available. In some cases we only need to query parameter types and
    /// don't need to resolve external references. This interface solves this problem by allowing our code to call 
    /// <c>GetParameters(RefResolutionMode.NoResolve)</c> in these cases.
    /// </remarks>
    public interface IParamsProvider
    {
        /// <summary>
        /// Gets parameters for the current method using the specified external references resolution mode
        /// </summary>
        /// <param name="refResolutionMode">External references resolution mode to use</param>
        /// <returns> An array of <see cref="ParameterInfo"/> containing information that matches the signature 
        /// of the current method</returns>
        ParameterInfo[] GetParameters(RefResolutionMode refResolutionMode);
    }
}
