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
                    /*CilGraph gr = CilGraph.Create((MethodBase)mi);
                    CilInstruction[] instructions = gr.GetInstructions().ToArray();

                    sb.AppendLine();
                    sb.AppendLine(" IL:");
                    for (int k = 0; k < instructions.Length;k++ )
                    {
                        CilInstruction instr = instructions[k];
                        if (instr.ByteOffset >= offset && instr.ByteOffset<=offset2)
                        {                                    
                            sb.AppendLine(instr.ToString());
                        } 
                    }
                    sb.AppendLine();*/

                    this.method = (MethodBase)mi;
                }
            } 
        }

        public MethodBase Method { get { return this.method; } }

        public int ILOffset { get { return this.il_start; } }

        public override string ToString()
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
                sb.AppendFormat("{0}!{1}", moduleDisplayName, this.frame.ToString());
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
