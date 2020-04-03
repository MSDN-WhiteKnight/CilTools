using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

namespace CilBytecodeParser
{
    public class CilDebugUtils
    {
        public static CilInstruction GetExecutingInstruction(StackFrame sf)
        {
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
                Debug.WriteLine(ex.ToString());
            }

            return retval;
        }

        public static CilInstruction GetLastExecutedInstruction(StackFrame sf)
        {
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
                Debug.WriteLine(ex.ToString());
            }

            return retval;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CilInstruction GetLastExecutedInstruction()
        {
            var trace = new StackTrace();
            var frame = trace.GetFrame(1);
            return GetLastExecutedInstruction(frame);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IEnumerable<CilInstruction> GetStackTrace()
        {
            var trace = new StackTrace();
            var frames = trace.GetFrames();
            for(int i=1;i<frames.Length;i++)
            {
                CilInstruction instr;

                if (i == 1) instr = CilDebugUtils.GetLastExecutedInstruction(frames[i]);
                else instr = CilDebugUtils.GetExecutingInstruction(frames[i]);

                if (instr == null) instr = CilInstruction.CreateEmptyInstruction(frames[i].GetMethod());

                yield return instr;
            }
        }

        

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PrintStackTrace()
        {           

            var trace = new StackTrace();
            var frames = trace.GetFrames();            

            for (int i = 1; i < frames.Length; i++)
            {
                CilInstruction instr;
                MethodBase mb=frames[i].GetMethod();

                if (i == 1) instr = CilDebugUtils.GetLastExecutedInstruction(frames[i]);
                else instr = CilDebugUtils.GetExecutingInstruction(frames[i]);

                if (instr == null) instr = CilInstruction.CreateEmptyInstruction(mb);                               

                Console.Write("in ");
                Console.Write(mb.DeclaringType.FullName + ".");
                Console.Write(mb.Name);
                Console.Write(" [" + instr.ByteOffset.ToString() + "]");
                Console.WriteLine(": " + instr.ToString());
            }
        }

        public static IEnumerable<CilInstruction> GetStackTrace(StackTrace trace)
        {            
            var frames = trace.GetFrames();
            for (int i = 0; i < frames.Length; i++)
            {
                CilInstruction instr;

                instr = CilDebugUtils.GetExecutingInstruction(frames[i]);

                if (instr == null) instr = CilInstruction.CreateEmptyInstruction(frames[i].GetMethod());

                yield return instr;
            }
        }

        public static void PrintStackTrace(StackTrace trace, TextWriter target = null)
        {
            var instructions = GetStackTrace(trace);

            if (target == null) target = Console.Out;

            foreach (CilInstruction instr in instructions)
            {
                target.Write("in ");                
                target.Write(instr.Method.DeclaringType.FullName + ".");
                target.Write(instr.Method.Name);
                target.Write(" [" + instr.ByteOffset.ToString() + "]"); 
                target.WriteLine( ": " + instr.ToString());
            }            
        }

        

    }
}
