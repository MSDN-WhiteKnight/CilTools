/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    /// <summary>
    /// Represents the information about the managed thread in the external process CLR instance
    /// </summary>
    public class ClrThreadInfo
    {
        ClrThread thread;
        ClrStackFrameInfo[] stacktrace;

        internal ClrThreadInfo(ClrThread th, IEnumerable<Assembly> assemblies, ClrAssemblyReader reader)
        {
            this.thread = th;
            IList<ClrStackFrame> stack = th.StackTrace;
            List<ClrStackFrameInfo> frames = new List<ClrStackFrameInfo>();

            for (int j = 0; j < stack.Count; j++)
            {
                ClrStackFrameInfo frame = new ClrStackFrameInfo(stack[j],assemblies,reader);
                frames.Add(frame);
            }

            this.stacktrace = frames.ToArray();
        }

        /// <summary>
        /// Gets the information about managed threads in the specified CLR instance 
        /// </summary>
        /// <param name="runtime">The CLR isntance to fetch thread information from</param>
        /// <param name="assemblies">
        /// The optional collection of assemblies preloaded from the target CLR instance, or null if you don't have any
        /// </param>
        /// <param name="reader">
        /// The optional <see cref="ClrAssemblyReader"/> object to be used for reading assemblies from the target CLR isntance, 
        /// or null if you don't have one
        /// </param>
        /// <returns>
        /// The array of <see cref="ClrThreadInfo"/> objects containing information about managed threads
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// The value of <paramref name="runtime"/> parameter is null 
        /// </exception>
        public static ClrThreadInfo[] Get(ClrRuntime runtime, IEnumerable<Assembly> assemblies, ClrAssemblyReader reader)
        {
            if (runtime == null) throw new ArgumentNullException("runtime");
            if (assemblies == null) assemblies = new Assembly[0];
            if (reader == null) reader = new ClrAssemblyReader(runtime);

            List<ClrThreadInfo> ret = new List<ClrThreadInfo>();

            for (int i = 0; i < runtime.Threads.Count; i++)
            {
                if (runtime.Threads[i].IsAlive == false || runtime.Threads[i].IsUnstarted) continue;

                ClrThreadInfo ti = new ClrThreadInfo(runtime.Threads[i],assemblies,reader);
                ret.Add(ti);
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Gets the stack trace of the current thread
        /// </summary>
        /// <remarks>
        /// The stack trace is the sequence of the nested method calls made in the thread before it reached the current point 
        /// of execution.
        /// </remarks>
        public IEnumerable<ClrStackFrameInfo> StackTrace
        {
            get
            {
                for (int i = 0; i < this.stacktrace.Length; i++)
                {
                    yield return this.stacktrace[i];
                }
            }
        }

        /// <summary>
        /// Gets the information about the current thread
        /// </summary>
        public string DisplayString
        {
            get
            {
                StringBuilder sb = new StringBuilder(200);
                sb.AppendFormat("ID: {0} ", thread.OSThreadId.ToString());

                if (thread.IsGC) sb.Append("[GC] ");
                if (thread.IsFinalizer) sb.Append("[Finalizer] ");
                if (thread.IsThreadpoolWorker) sb.Append("[Thread pool worker] ");
                if (thread.IsThreadpoolCompletionPort) sb.Append("[Thread pool completion port] ");
                if (thread.IsDebuggerHelper) sb.Append("[Debug] ");

                if (thread.IsMTA) sb.Append("[MTA] ");
                else if (thread.IsSTA) sb.Append("[STA] ");

                if (this.stacktrace.Length > 0)
                {
                    string framestr = null;

                    for (int i = 0; i < this.stacktrace.Length; i++)
                    {
                        if (this.stacktrace[i].Method != null)
                        {
                            framestr = this.stacktrace[i].ToString(false);
                            break;
                        }
                    }

                    if (framestr == null) framestr = this.stacktrace[0].ToString(false);

                    sb.Append(" In: ");
                    sb.Append(framestr);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Prints the information about the current thread to the specified TextWriter
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// The value of <paramref name="output"/> parameter is null 
        /// </exception>
        public void Print(TextWriter output)
        {
            if (output == null) throw new ArgumentNullException("output");

            output.WriteLine(this.DisplayString);

            if (this.stacktrace.Length > 0) output.WriteLine("Stack trace:");

            for (int j = 0; j < this.stacktrace.Length; j++)
            {
                output.Write(' ');
                output.WriteLine(this.stacktrace[j].ToString());
            }
        }

        /// <summary>
        /// Gets the information about the current thread
        /// </summary>
        public override string ToString()
        {
            return this.DisplayString;
        }

    }
}
