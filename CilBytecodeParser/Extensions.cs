/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CilBytecodeParser;

namespace CilBytecodeParser.Extensions
{
    /// <summary>
    /// A collection of extension methods that provide an alternative syntax for some static methods of this library
    /// </summary>
    public static class CilExtensions
    {
        /// <summary>
        /// Returns CIL graph that represents this method
        /// </summary>
        /// <param name="m">Method for which to build CIL graph</param>
        /// <returns>CIL graph object</returns>
        public static CilGraph GetCilGraph(this MethodBase m)
        {
            return CilAnalysis.GetGraph(m);
        }

        /// <summary>
        /// Returns this method's CIL code as string
        /// </summary>
        /// <param name="m">Method for which to retreive CIL</param>
        /// <returns>CIL code string</returns>
        public static string GetCilText(this MethodBase m)
        {
            return CilAnalysis.MethodToText(m);
        }

        /// <summary>
        /// Retrieves all instructions the method's body
        /// </summary>
        /// <param name="m">Source method</param>
        /// <returns>A collection of CIL instructions that form the body of this method</returns>
        public static IEnumerable<CilInstruction> GetInstructions(this MethodBase m)
        {
            return CilReader.GetInstructions(m);
        }

        /// <summary>
        /// Gets an currently executing instruction corresponding to this stack frame
        /// </summary>
        /// <param name="sf">A stack frame object</param>
        /// <returns>CIL instruction</returns>
        public static CilInstruction GetExecutingInstruction(this StackFrame sf)
        {
            return DebugUtils.GetExecutingInstruction(sf);
        }

        /// <summary>
        /// Gets a last executed instruction corresponding to this stack frame
        /// </summary>
        /// <param name="sf">A stack frame object</param>
        /// <returns>CIL instruction</returns>
        public static CilInstruction GetLastExecutedInstruction(this StackFrame sf)
        {
            return DebugUtils.GetLastExecutedInstruction(sf);
        }

        /// <summary>
        /// Gets a repesentation of this stack trace as CIL instructions
        /// </summary>
        /// <param name="trace">Stack trace object</param>
        /// <returns>A collection of CIL instructions</returns>
        public static IEnumerable<CilInstruction> GetInstructions(this StackTrace trace)
        {
            return DebugUtils.GetStackTrace(trace);
        }

        /// <summary>
        /// Prints this stack trace, represented as a CIL code, into the specified TextWriter
        /// </summary>
        /// <param name="trace">Source stack trace object</param>
        /// <param name="target">Target TextWriter object. If null or omitted, standard output will be used.</param>
        public static void PrintInstructions(this StackTrace trace, System.IO.TextWriter target = null)
        {
            DebugUtils.PrintStackTrace(trace, target);
        }

        /// <summary>
        /// Gets all methods that are referenced by this method
        /// </summary>
        /// <param name="mb">Method for which to retreive referenced methods</param>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(this MethodBase mb)
        {
            return CilAnalysis.GetReferencedMethods(mb);
        }

        /// <summary>
        /// Get all methods that are referenced by the code of this type
        /// </summary>
        /// <param name="t">Type for which to retreive referenced methods</param>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(this Type t)
        {
            return CilAnalysis.GetReferencedMethods(t);
        }

        /// <summary>
        /// Get all methods that are referenced by the code in the specified assembly
        /// </summary>
        /// <param name="ass">Assembly for which to retreive referenced methods</param>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(this Assembly ass)
        {
            return CilAnalysis.GetReferencedMethods(ass);
        }
    }
}
