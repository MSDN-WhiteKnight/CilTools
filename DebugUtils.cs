/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

namespace CilBytecodeParser
{
    /// <summary>
    /// A collection of utility methods to assist in debugging
    /// </summary>
    public static class DebugUtils
    {
        /// <summary>
        /// Raised when error occurs in one of the methods in this class
        /// </summary>
        public static event EventHandler<CilErrorEventArgs> Error;

        static void OnError(object sender, CilErrorEventArgs e)
        {
            EventHandler<CilErrorEventArgs> handler = Error;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// Gets an currently executing instruction corresponding to the specified stack frame
        /// </summary>
        /// <param name="sf">A stack frame object</param>
        /// <exception cref="System.ArgumentNullException">Source stack frame is null</exception>
        /// <returns>CIL instruction</returns>
        public static CilInstruction GetExecutingInstruction(StackFrame sf)
        {
            if (sf == null) throw new ArgumentNullException("sf", "Source stack frame cannot be null");

            CilInstruction instr=null, retval=null;

            try
            {
                MethodBase mb = sf.GetMethod();
                CilReader reader = new CilReader(mb);

                while (true)
                {
                    if (reader.State != CilReaderState.Reading) break;

                    instr = reader.Read();

                    uint offset = (uint)(sf.GetILOffset() );

                    if (instr.ByteOffset + instr.OpCode.Size + instr.OperandSize >= offset)
                    {
                        retval = instr;
                        if (retval != null && !retval.OpCode.Equals(OpCodes.Nop)) break;
                    }                    
                }
            }
            catch (Exception ex)
            {
                string error = "Exception occured when trying to get executing exception from stack frame.";
                OnError(sf, new CilErrorEventArgs(ex, error));  
            }

            return retval;
        }

        /// <summary>
        /// Gets a last executed instruction corresponding to the specified stack frame
        /// </summary>
        /// <param name="sf">A stack frame object</param>
        /// <exception cref="System.ArgumentNullException">Source stack frame is null</exception>
        /// <returns>CIL instruction</returns>
        public static CilInstruction GetLastExecutedInstruction(StackFrame sf)
        {
            if (sf == null) throw new ArgumentNullException("sf", "Source stack frame cannot be null");

            CilInstruction instr = null, prev_instr = null, retval = null;

            try
            {
                MethodBase mb = sf.GetMethod();
                CilReader reader = new CilReader(mb);

                while (true)
                {
                    if (reader.State != CilReaderState.Reading) break;

                    instr = reader.Read();

                    uint offset = (uint)(sf.GetILOffset());

                    if (instr.ByteOffset + instr.OpCode.Size + instr.OperandSize >= offset)
                    {
                        retval = prev_instr;
                        break;
                    }

                    if (!instr.OpCode.Equals(OpCodes.Nop))
                    {
                        prev_instr = instr;
                    }
                }
            }
            catch (Exception ex)
            {
                string error = "Exception occured when trying to get last executed exception from stack frame.";
                OnError(sf, new CilErrorEventArgs(ex, error));  
            }

            return retval;
        }

        /// <summary>
        /// Gets a last executed instruction at the calling point of the code
        /// </summary>
        /// <returns>CIL instruction</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CilInstruction GetLastExecutedInstruction()
        {
            var trace = new StackTrace();
            var frame = trace.GetFrame(1);
            return GetLastExecutedInstruction(frame);
        }

        /// <summary>
        /// Gets a stack trace at the calling point represented as CIL instructions
        /// </summary>
        /// <returns>A collection of CIL instructions corresponding to frames of a callstack</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IEnumerable<CilInstruction> GetStackTrace()
        {
            var trace = new StackTrace();
            var frames = trace.GetFrames();
            for(int i=1;i<frames.Length;i++)
            {
                CilInstruction instr;

                if (i == 1) instr = DebugUtils.GetLastExecutedInstruction(frames[i]);
                else instr = DebugUtils.GetExecutingInstruction(frames[i]);

                if (instr == null) instr = CilInstruction.CreateEmptyInstruction(frames[i].GetMethod());

                yield return instr;
            }
        }
        
        /// <summary>
        /// Prints a stack trace at the calling point, represented as a CIL code, into the standard output
        /// </summary>        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PrintStackTrace()
        {           

            var trace = new StackTrace();
            var frames = trace.GetFrames();            

            for (int i = 1; i < frames.Length; i++)
            {
                CilInstruction instr;
                MethodBase mb=frames[i].GetMethod();

                if (i == 1) instr = DebugUtils.GetLastExecutedInstruction(frames[i]);
                else instr = DebugUtils.GetExecutingInstruction(frames[i]);

                if (instr == null) instr = CilInstruction.CreateEmptyInstruction(mb);                               

                Console.Write("in ");
                Console.Write(mb.DeclaringType.FullName + ".");
                Console.Write(mb.Name);
                Console.Write(" [#" + instr.OrdinalNumber.ToString() + "]");
                Console.WriteLine(": " + instr.ToString());
            }
        }

        /// <summary>
        /// Gets a repesentation of the call stack as CIL instructions
        /// </summary>
        /// <param name="trace">Stack trace object</param>
        /// <exception cref="System.ArgumentNullException">Source stack trace is null</exception>
        /// <returns>A collection of CIL instructions</returns>
        public static IEnumerable<CilInstruction> GetStackTrace(StackTrace trace)
        {
            if (trace == null) throw new ArgumentNullException("trace", "Source stack trace cannot be null");

            var frames = trace.GetFrames();
            for (int i = 0; i < frames.Length; i++)
            {
                CilInstruction instr;

                instr = DebugUtils.GetExecutingInstruction(frames[i]);

                if (instr == null) instr = CilInstruction.CreateEmptyInstruction(frames[i].GetMethod());

                yield return instr;
            }
        }

        /// <summary>
        /// Prints a stack trace, represented as a CIL code, into the specified TextWriter
        /// </summary>
        /// <param name="trace">Source stack trace object</param>
        /// <param name="target">Target TextWriter object. If null or omitted, standard output will be used.</param>
        /// <exception cref="System.ArgumentNullException">Source stack trace is null</exception>
        public static void PrintStackTrace(StackTrace trace, TextWriter target = null)
        {            
            var instructions = GetStackTrace(trace);

            if (target == null) target = Console.Out;

            foreach (CilInstruction instr in instructions)
            {
                target.Write("in ");                
                target.Write(instr.Method.DeclaringType.FullName + ".");
                target.Write(instr.Method.Name);
                target.Write(" [#" + instr.OrdinalNumber.ToString() + "]"); 
                target.WriteLine( ": " + instr.ToString());
            }            
        }
    }
}
