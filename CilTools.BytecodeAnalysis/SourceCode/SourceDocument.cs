/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CilTools.SourceCode
{
    /// <summary>
    /// Contains information about source code document
    /// </summary>
    public class SourceDocument
    {
        List<SourceFragment> fragments;
        Dictionary<string, object> data;

        /// <summary>
        /// Creates a new empty instance of <c>SourceDocument</c>
        /// </summary>
        public SourceDocument()
        {
            this.FilePath = string.Empty;
            this.SymbolsFile = string.Empty;
            this.fragments = new List<SourceFragment>();
        }

        /// <summary>
        /// Gets or sets a path to the file from which this document is loaded
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets a path to the symbols file from which the sequence points data for this document 
        /// is loaded
        /// </summary>
        public string SymbolsFile { get; set; }

        /// <summary>
        /// Gets or sets a method which source code is contained in this document
        /// </summary>
        public MethodBase Method { get; set; }

        /// <summary>
        /// Adds the specified fragment into this document
        /// </summary>
        /// <param name="fragment">A source code fragment to add</param>
        public void AddFragment(SourceFragment fragment)
        {
            this.fragments.Add(fragment);
            fragment.SetOwnerDocument(this);
        }

        /// <summary>
        /// Gets source code fragments contained in this document
        /// </summary>
        public IEnumerable<SourceFragment> Fragments
        {
            get 
            {
                foreach (SourceFragment fragment in this.fragments)
                {
                    yield return fragment;
                }
            }
        }

        /// <summary>
        /// Sets an additional implementation-defined information about this document
        /// </summary>        
        public void SetAdditionalInfo(string name, object val) 
        {
            if (this.data == null) this.data = new Dictionary<string, object>();

            this.data[name] = val;
        }

        /// <summary>
        /// Gets an additional implementation-defined information about this document
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
