/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;

namespace CilTools.SourceCode
{
    /// <summary>
    /// Contains information about the source code fragment
    /// </summary>
    public class SourceFragment : FragmentBase
    {
        SourceDocument owner;

        /// <summary>
        /// Gets the source document which this fragment belongs to
        /// </summary>
        public SourceDocument Document { get { return this.owner; } }

        internal void SetOwnerDocument(SourceDocument doc)
        {
            this.owner = doc;
        }
        
        /// <summary>
        /// Gets or sets the starting byte offset of CIL bytecode corresponding to this fragment 
        /// </summary>
        public int CilStart { get; set; }

        /// <summary>
        /// Gets or sets the ending byte offset of CIL bytecode corresponding to this fragment 
        /// </summary>
        public int CilEnd { get; set; }

        /// <summary>
        /// Gets a method which source code is contained in this fragment
        /// </summary>
        public MethodBase Method 
        { 
            get 
            {
                if (this.owner != null) return this.owner.Method;
                else return null;
            }
        }
    }
}
