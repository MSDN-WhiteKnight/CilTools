/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace CilTools.BytecodeAnalysis.Extensions
{
    /// <summary>
    /// A collection of extension methods that provide an alternative syntax for some static methods of this library
    /// </summary>
    public static class CilExtensions
    {
        /// <summary>
        /// Returns <see cref="CilGraph"/> that represents this method
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
        /// <param name="m">Method for which to retrieve CIL</param>
        /// <remarks>Alias for <see cref="CilAnalysis.MethodToText"/> method. The CIL code returned by this API is intended mainly for reading, not compiling. It is not guaranteed to be a valid input for CIL assembler.</remarks>
        /// <returns>CIL code string</returns>
        public static string GetCilText(this MethodBase m)
        {
            return CilAnalysis.MethodToText(m);
        }

        /// <summary>
        /// Retrieves all instructions from the method's body
        /// </summary>
        /// <param name="m">Source method</param>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Failed to retrieve method body for the method</exception>
        /// <returns>A collection of CIL instructions that form the body of this method</returns>
        public static IEnumerable<CilInstructionBase> GetInstructions(this MethodBase m)
        {
            return CilReader.GetInstructions(m);
        }

        /// <summary>
        /// Gets an currently executing instruction corresponding to this stack frame
        /// </summary>
        /// <param name="sf">A stack frame object</param>
        /// <returns>CIL instruction</returns>
        public static CilInstructionBase GetExecutingInstruction(this StackFrame sf)
        {
            return DebugUtils.GetExecutingInstruction(sf);
        }

        /// <summary>
        /// Gets a last executed instruction corresponding to this stack frame
        /// </summary>
        /// <param name="sf">A stack frame object</param>
        /// <returns>CIL instruction</returns>
        public static CilInstructionBase GetLastExecutedInstruction(this StackFrame sf)
        {
            return DebugUtils.GetLastExecutedInstruction(sf);
        }

        /// <summary>
        /// Gets a repesentation of this stack trace as CIL instructions
        /// </summary>
        /// <param name="trace">Stack trace object</param>
        /// <returns>A collection of CIL instructions</returns>
        public static IEnumerable<CilInstructionBase> GetInstructions(this StackTrace trace)
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
        /// <param name="mb">Method for which to retrieve referenced methods</param>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Failed to retrieve method body for the method</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in source method's body. For example, if the source method calls `Foo` method or creates delegate pointing to `Foo`, `Foo` is referenced by the source method.</remarks>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(this MethodBase mb)
        {
            return CilAnalysis.GetReferencedMethods(mb);
        }

        /// <summary>
        /// Get all methods that are referenced by the code of this type
        /// </summary>
        /// <param name="t">Type for which to retrieve referenced methods</param>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the type's methods.</remarks>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(this Type t)
        {
            return CilAnalysis.GetReferencedMethods(t);
        }

        /// <summary>
        /// Get all methods that are referenced by the code in the specified assembly
        /// </summary>
        /// <param name="ass">Assembly for which to retrieve referenced methods</param>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the assembly's methods.</remarks>
        /// <returns>A collection of referenced methods</returns>
        public static IEnumerable<MethodBase> GetReferencedMethods(this Assembly ass)
        {
            return CilAnalysis.GetReferencedMethods(ass);
        }

        /// <summary>
        /// Gets all members (fields or methods) referenced by specified method
        /// </summary>
        /// <param name="mb">Method for which to retrieve referenced members</param>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Failed to retrieve method body for the method</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in source method's body. For example, if the source method calls `Foo` method or creates delegate pointing to `Foo`, `Foo` is referenced by the source method.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(this MethodBase mb)
        {
            return CilAnalysis.GetReferencedMembers(mb);
        }

        /// <summary>
        /// Gets all members referenced by the code of specified type
        /// </summary>
        /// <param name="t">Type for which to retrieve referenced memmbers</param>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the type's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(this Type t)
        {
            return CilAnalysis.GetReferencedMembers(t);
        }

        /// <summary>
        /// Gets all members referenced by the code of specified assembly
        /// </summary>
        /// <param name="ass">Assembly for which to retrieve referenced members</param>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the assembly's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(this Assembly ass)
        {
            return CilAnalysis.GetReferencedMembers(ass);
        }

        /// <summary>
        /// Gets members (fields or methods) referenced by specified method that match specified criteria
        /// </summary>
        /// <param name="mb">Method for which to retrieve referenced members</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retrieved</param>
        /// <exception cref="System.NotSupportedException">CilReader encountered unknown opcode</exception>
        /// <exception cref="CilParserException">Failed to retrieve method body for the method</exception>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in source method's body. For example, if the source method calls `Foo` method or creates delegate pointing to `Foo`, `Foo` is referenced by the source method.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(this MethodBase mb, MemberCriteria flags)
        {
            return CilAnalysis.GetReferencedMembers(mb, flags);
        }

        /// <summary>
        /// Gets members referenced by the code of specified type that match specified criteria
        /// </summary>
        /// <param name="t">Type for which to retrieve referenced memmbers</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retrieved</param>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the type's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(this Type t, MemberCriteria flags)
        {
            return CilAnalysis.GetReferencedMembers(t, flags);
        }

        /// <summary>
        /// Gets members referenced by the code of specified assembly that match specified criteria
        /// </summary>
        /// <param name="ass">Assembly for which to retrieve referenced members</param>
        /// <param name="flags">A combination of bitwise flags that control what kind of members are retrieved</param>
        /// <remarks>Referenced member is a member that appears as an operand of instruction in any of the assembly's methods.</remarks>
        /// <returns>A collection of MemberInfo objects</returns>
        public static IEnumerable<MemberInfo> GetReferencedMembers(this Assembly ass, MemberCriteria flags)
        {
            return CilAnalysis.GetReferencedMembers(ass, flags);
        }

#if !NETSTANDARD
        /// <summary>
        /// Emits CIL code for the specified instruction into the specified IL generator.
        /// </summary>
        /// <param name="ilg">Target IL generator.</param>
        /// <param name="instr">IL instruction to be emitted.</param>
        public static void EmitInstruction(this ILGenerator ilg, CilInstructionBase instr)
        {
            instr.EmitTo(ilg);
        }


        /// <summary>
        /// Emits the entire content of CIL graph into the specified IL generator, 
        /// optionally calling user callback for each processed instruction.
        /// </summary>
        /// <param name="ilg">Target IL generator. </param>
        /// <param name="graph">The CIL graph which content should be emitted.</param>
        /// <param name="callback">User callback to be called for each processed instruction.</param>
        /// <remarks>Passing user callback into this method enables you to filter instructions that you want to be emitted 
        /// into target IL generator. 
        /// Return <see langword="true"/> to skip emitting instruction, or <see langword="false"/> to emit instruction.</remarks>
        public static void EmitCilGraph(this ILGenerator ilg, CilGraph graph, Func<CilInstructionBase, bool> callback = null)
        {
            graph.EmitTo(ilg, callback);
        }
#endif
    }
}
