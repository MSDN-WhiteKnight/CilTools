/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;

namespace CilTools.Reflection
{
    /// <summary>
    /// Provides additional information about reflection objects
    /// </summary>
    public interface IReflectionInfo
    {
        object GetReflectionProperty(string propertyName);

        object GetReflectionProperty(int id);

        IEnumerable<string> EnumReflectionProperties();

        IEnumerable<int> EnumReflectionPropertyIds();

        bool HasReflectionProperty(string propertyName);

        bool HasReflectionProperty(int id);
    }

    /// <summary>
    /// Provides constants for reflection properties used with <see cref="IReflectionInfo"/>
    /// </summary>
    public static class ReflectionInfoProperties
    {
        /// <summary>
        /// Represents the interface method implemented by explicit interface implementation
        /// </summary>
        public const int ExplicitlyImplementedMethod = 100;
    }
}
