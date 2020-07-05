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

                return sb.ToString();
            }
        }

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

        public override string ToString()
        {
            return this.DisplayString;
        }

    }
}
