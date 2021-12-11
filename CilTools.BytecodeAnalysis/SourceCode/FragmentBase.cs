/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.SourceCode
{
    /// <summary>
    /// Provides a base class for source code fragments
    /// </summary>
    public abstract class FragmentBase
    {
        Dictionary<string, object> data;

        internal FragmentBase() 
        {
            this.Text = string.Empty;
        }

        /// <summary>
        /// Gets or sets the source code of this fragment
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the line number (zero-based) where this fragment starts in the source file
        /// </summary>
        public int LineStart { get; set; }

        /// <summary>
        /// Gets or sets the line number (zero-based) where this fragment ends in the source file
        /// </summary>
        public int LineEnd { get; set; }

        /// <summary>
        /// Gets or sets the column number (zero-based) where this fragment starts in the source file
        /// </summary>
        public int ColStart { get; set; }

        /// <summary>
        /// Gets or sets the column number (zero-based) where this fragment ends in the source file
        /// </summary>
        public int ColEnd { get; set; }

        /// <summary>
        /// Sets an additional implementation-defined information about this fragment
        /// </summary>        
        public void SetAdditionalInfo(string name, object val)
        {
            if (this.data == null) this.data = new Dictionary<string, object>();

            this.data[name] = val;
        }

        /// <summary>
        /// Gets an additional implementation-defined information about this fragment
        /// </summary>
        /// <returns>The requested value, or null if it is not set</returns>
        public object GetAdditionalInfo(string name)
        {
            if (this.data == null) return null;

            object ret;

            if (this.data.TryGetValue(name, out ret)) return ret;
            else return null;
        }
    }
}
