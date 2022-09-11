/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Reflection
{
    /// <summary>
    /// Provides additional information about reflection objects
    /// </summary>
    public interface IReflectionInfo
    {
        /// <summary>
        /// Gets the value of the reflection property with the specified Id
        /// </summary>
        /// <param name="id">Property Id to get</param>
        /// <returns>
        /// The property value, or null if property is not supported
        /// </returns>
        object GetReflectionProperty(int id);
    }

    /// <summary>
    /// Provides constants for reflection properties used with <see cref="IReflectionInfo"/>
    /// </summary>
    public static class ReflectionInfoProperties
    {
        /// <summary>
        /// Represents the string containing information about object
        /// </summary>
        public const int InfoText = 1;

        /// <summary>
        /// Represents the array of external modules referenced by assembly (Type: string[])
        /// </summary>
        public const int ReferencedModules = 100;

        /// <summary>
        /// Gets the value of the reflection property with the specified Id from the specified object
        /// </summary>
        /// <param name="obj">Reflection object to get property</param>
        /// <param name="id">Property Id</param>
        /// <returns>The property value, or null if property is not supported</returns>
        public static object GetProperty(object obj, int id)
        {
            IReflectionInfo info = obj as IReflectionInfo;

            if (info == null) return null;
            else return info.GetReflectionProperty(id);
        }
    }
}
