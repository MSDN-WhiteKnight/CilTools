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
    /// Represents a single frame in the callstack of .NET application. The stack frame contains information about the called method 
    /// and the location of the IL code executed within that method
    /// </summary>
    public class ClrStackFrameInfo
    {
        ClrStackFrame frame;
        MethodBase method;
        int il_start=-1;
        int il_end=Int32.MaxValue;

        internal ClrStackFrameInfo(ClrStackFrame f,IEnumerable<Assembly> assemblies, ClrAssemblyReader reader)
        {
            this.frame = f;

            ClrType clrtype = null;
            ClrModule clrmodule = null;            
            string module = f.ModuleName;
            ClrMethod m = f.Method;

            if (module == null) module = "";
            if (m != null) clrtype = m.Type;
            if (clrtype != null) clrmodule = clrtype.Module;
                        
            if (m != null && this.frame.Kind == ClrStackFrameType.ManagedMethod)
            {
                //find IL offset
                ulong pos = this.frame.InstructionPointer;

                ILToNativeMap[] map = m.ILOffsetMap;
                if (map == null) map = new ILToNativeMap[0];

                int offset = -1;
                int offset2 = Int32.MaxValue;
                bool found = false;

                for (int k = 0; k < map.Length; k++)
                {
                    if (pos < map[k].EndAddress && pos >= map[k].StartAddress)
                    {
                        offset = map[k].ILOffset;
                        if (k < map.Length - 1) offset2 = map[k + 1].ILOffset;
                        found = true;
                    }
                }

                if (found)
                {
                    this.il_start = offset;
                    this.il_end = offset2;
                }

                //find assembly that contains method
                Assembly ass = null;

                foreach(Assembly a in assemblies)
                {
                    string an = a.GetName().Name;

                    if (String.Equals(an, module, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ass = a;
                        break;
                    }
                }

                //find method
                Type t = null;
                MemberInfo mi = null;
                string tn = m.Type.Name;

                if (ass != null && ass is ClrAssemblyInfo)
                {
                    //find method by token
                    mi = ((ClrAssemblyInfo)ass).ResolveMember((int)m.MetadataToken);
                }
                else
                {
                    //find method by name
                    if (ass != null && !String.IsNullOrEmpty(tn))
                    {
                        t = ass.GetType(tn);
                    }

                    if (t != null)
                    {
                        MemberInfo[] arr = t.GetMember(m.Name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                            );

                        if (arr.Length == 1)
                        {
                            mi = arr[0];
                        }
                    }
                }

                //find method by token via ClrMD
                if (mi == null && clrmodule != null && reader!=null)
                {
                    ClrAssemblyInfo ass2 = reader.Read(clrmodule);
                    mi = ass2.ResolveMember((int)m.MetadataToken);
                }

                if (mi != null && mi is MethodBase)
                {
                    this.method = (MethodBase)mi;
                }
            } 
        }

        /// <summary>
        /// Gets the method corresponding to this stack frame. The value could be null if this frame is a special frame not 
        /// corresponding to a managed method, or if the library failed to construct MethodBase object for the called method.
        /// </summary>
        public MethodBase Method { get { return this.method; } }

        /// <summary>
        /// Gets the offset, in bytes, of the beginning of the IL code executed by this frame, relative to the beginning of the method body. 
        /// </summary>
        /// <remarks>
        /// The library cannot accurately determine the currently executed instruction in every case, because the resulting native code 
        /// could be optimized by JIT. As a result, the currently executed code is presented as the starting and ending offsets.
        /// </remarks>
        public int ILOffset { get { return this.il_start; } }

        /// <summary>
        /// Gets the offset, in bytes, of the end of the IL code executed by this frame, relative to the beginning of the method body. 
        /// </summary>
        /// <remarks>
        /// The library cannot accurately determine the currently executed instruction in every case, because the resulting native code 
        /// could be optimized by JIT. As a result, the currently executed code is presented as the starting and ending offsets.
        /// </remarks>
        public int ILOffsetEnd { get { return this.il_end; } }

        /// <summary>
        /// Gets the full text representation of this stack frame 
        /// </summary>
        /// <remarks>
        /// The full text representation contains full signature of the called method, including argument types. 
        /// If this frame is not a call to a managed method, returns the name of the special frame from debugger API.
        /// </remarks>
        public override string ToString()
        {
            return this.ToString(true);
        }

        /// <summary>
        /// Gets the text representation of this stack frame 
        /// </summary>
        /// <param name="full">Indicates whether the method should return full text representation</param>
        /// <remarks>
        /// The full text representation contains full signature of the called method, including argument types. 
        /// The short text representation only contains the method's name. 
        /// If this frame is not a call to a managed method, returns the name of the special frame from debugger API.
        /// </remarks>
        public string ToString(bool full)
        {
            ClrType clrtype = null;
            ClrModule clrmodule = null;
            string moduleDisplayName = null;
            string module = this.frame.ModuleName;
            ClrMethod m = this.frame.Method;
            StringBuilder sb = new StringBuilder(100);

            if (module == null) module = "";
            if (m != null) clrtype = m.Type;
            if (clrtype != null) clrmodule = clrtype.Module;

            //try get module file name with extension
            if (clrmodule != null && clrmodule.IsFile) moduleDisplayName = Path.GetFileName(clrmodule.FileName);
            if (String.IsNullOrEmpty(moduleDisplayName)) moduleDisplayName = module;

            //print stack frame
            if ((this.frame.Kind == ClrStackFrameType.Runtime || this.frame.Kind == ClrStackFrameType.Unknown) &&
                m == null)
            {
                sb.Append(this.frame.ToString());
            }
            else
            {
                string name = "";

                if (full)
                {
                    name = this.frame.ToString();
                }
                else
                {
                    if (m != null)
                    {
                        if (clrtype != null) name += clrtype.Name + ".";

                        name += m.Name;
                    }
                    else name = this.frame.ToString();
                }

                sb.AppendFormat("{0}!{1}", moduleDisplayName, name);
            }

            //print IL offset
            if (this.frame.Kind == ClrStackFrameType.ManagedMethod && this.il_start >= 0)
            {
                sb.Append('+');
                sb.Append(this.il_start.ToString());
            }

            return sb.ToString();
        }
    }
}
