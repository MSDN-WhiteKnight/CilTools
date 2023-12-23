﻿/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Visualization
{
    /// <summary>
    /// Contains options that control the visualization process performed by <see cref="SyntaxVisualizer"/>
    /// </summary>
    public class VisualizationOptions
    {
        Dictionary<string, object> _dict = new Dictionary<string, object>();

        public VisualizationOptions()
        {
            this.EnableSyntaxHighlighting = true;
        }
        
        public object GetOption(string name)
        {
            object val;

            if (_dict.TryGetValue(name, out val)) return val;
            else return null;
        }

        public void SetOption(string name, object val)
        {
            this._dict[name] = val;
        }

        public int HighlightingStartOffset
        {
            get
            {
                object val = this.GetOption("HighlightingStartOffset");

                if (val != null) return (int)val;
                else return -1;
            }

            set { this.SetOption("HighlightingStartOffset", value); }
        }

        public int HighlightingEndOffset
        {
            get
            {
                object val = this.GetOption("HighlightingEndOffset");

                if (val != null) return (int)val;
                else return -1;
            }

            set { this.SetOption("HighlightingEndOffset", value); }
        }

        internal bool EnableInstructionDoubleClick
        {
            get 
            {
                object val = this.GetOption("EnableInstructionDoubleClick");

                if (val != null) return (bool)val;
                else return false;
            }
        }

        internal bool EnableMethodDefinitionLinks
        {
            get
            {
                object val = this.GetOption("EnableMethodDefinitionLinks");

                if (val != null) return (bool)val;
                else return false;
            }
        }

        public bool EnableInstructionNavigation
        {
            get
            {
                object val = this.GetOption("EnableInstructionNavigation");

                if (val != null) return (bool)val;
                else return false;
            }

            set { this.SetOption("EnableInstructionNavigation", value); }
        }

        /// <summary>
        /// Gets or sets the boolean value indicating whether the syntax highlighting is enabled
        /// </summary>
        /// <remarks>
        /// The default value is <c>true</c>. This property is honoured by HTML and ConsoleText visualizers.
        /// </remarks>
        public bool EnableSyntaxHighlighting
        {
            get
            {
                object val = this.GetOption("EnableSyntaxHighlighting");

                if (val != null) return (bool)val;
                else return false;
            }

            set { this.SetOption("EnableSyntaxHighlighting", value); }
        }
    }
}
