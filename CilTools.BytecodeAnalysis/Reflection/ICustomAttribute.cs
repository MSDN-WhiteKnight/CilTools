/* CilTools.BytecodeAnalysis library
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents the custom attribute data
    /// </summary>
    /// <remarks>
    /// This interface is needed to support fetching custom attribute data in reflection-only mode 
    /// in .NET Framework 3.5. The library treats objects that implement <c>ICustomAttribute</c> in a 
    /// special way when processing an array returned by <c>MethodBase.GetCustomAttributes</c>, 
    /// fetching raw attribute data instead of attempting to emulate that data based on what reflection 
    /// returns for an attribute type. Implement this interface when you need to pass your custom method 
    /// object to APIs like <see cref="CilTools.BytecodeAnalysis.CilGraph"/> and make it possible to 
    /// read raw data of method's custom attributes.
    /// </remarks>
    public interface ICustomAttribute
    {
        /// <summary>
        /// Gets the method this attribute is attached to
        /// </summary>
        MethodBase Owner { get; }

        /// <summary>
        /// Gets the constructor used to instantiate object of this attribute
        /// </summary>
        MethodBase Constructor { get; }

        /// <summary>
        /// Gets the raw attribute data as the byte array
        /// </summary>
        /// <remarks>
        /// The format of the byte array is defined by ECMA-335 specification, paragraph II.23.3 - 
        /// Custom attributes. The data in the byte array specifies constructor's arguments and 
        /// property values used to create an object for this attribute.
        /// </remarks>
        byte[] Data { get; }
    }
}
