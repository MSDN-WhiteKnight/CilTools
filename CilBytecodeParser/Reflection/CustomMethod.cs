﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    /// <summary>
    /// A base class for MethodBase implementations providing custom mechanisms for extracting bytecode data. 
    /// Inherit from this class when you want CilBytecodeparser to process bytecode from your custom data source, instead of 
    /// reflection.
    /// </summary>
    internal abstract class CustomMethod : MethodBase
    {
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
        /// Converts MethodBase into the form suitable for processing by CilBytecodeparser
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
