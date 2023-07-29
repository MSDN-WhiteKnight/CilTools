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
    public static class ReflectionProperties
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
        /// Represents the preferred base address for the PE image (Type: ulong)
        /// </summary>
        public const int ImageBase = 101;

        /// <summary>
        /// Represents the PE image alignment factor, in bytes (Type: int)
        /// </summary>
        public const int FileAlignment = 102;

        /// <summary>
        /// Represents the PE image reserved stack size, in bytes (Type: ulong)
        /// </summary>
        public const int StackReserve = 103;

        /// <summary>
        /// Represents the PE image subsystem (Type: int)
        /// </summary>
        public const int Subsystem = 104;

        /// <summary>
        /// Represents the PE image runtime flags (Type: int)
        /// </summary>
        public const int CorFlags = 105;

        /// <summary>
        /// Indicates whether the member is static (Type: bool)
        /// </summary>
        public const int IsStatic = 106;

        /// <summary>
        /// Represents interface methods implemented by this method, if this method is an explicit interface
        /// implementation. Otherwise, should be an empty array. (Type: MethodInfo[])
        /// </summary>
        public const int ExplicitlyImplementedMethods = 107;

        /// <summary>
        /// Represents the information about VTable entry associated with this method, or an empty 
        /// string if the method does not have a VTable entry. (Type: string)
        /// </summary>
        public const int VTableEntry = 108;

        /// <summary>
        /// Represents a method signature. (Type: Signature)
        /// </summary>
        public const int Signature = 109;

        /// <summary>
        /// Represents a Relative Virtual Address (RVA) of a field mapped to an executable image memory block. (Type: int)
        /// </summary>
        public const int FieldRva = 110;

        /// <summary>
        /// Represents a byte array containing RVA field's value. (Type: byte[])
        /// </summary>
        public const int RvaFieldValue = 111;

        /// <summary>
        /// Represents a field offset from the beginning of the structure, for structures with explicit layout. 
        /// Returns -1 when field offset is not available. (Type: int)
        /// </summary>
        public const int FieldOffset = 112;

        /// <summary>
        /// Represents an array of virtual function tables in the executable image. (Type: VTable[])
        /// </summary>
        public const int VTables = 113;

        /// <summary>
        /// Represents an array of forwarded types. (Type: Type[])
        /// </summary>
        public const int ForwardedTypes = 114;

        /// <summary>
        /// Gets the value of the reflection property with the specified Id from the specified object
        /// </summary>
        /// <param name="obj">Reflection object to get property</param>
        /// <param name="id">Property Id</param>
        /// <returns>The property value, or null if property is not supported</returns>
        public static object Get(object obj, int id)
        {
            IReflectionInfo info = obj as IReflectionInfo;

            if (info == null) return null;
            else return info.GetReflectionProperty(id);
        }
    }
}
