/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Visualization
{
    public class VisualizationOptions
    {
        Dictionary<string, object> _dict = new Dictionary<string, object>();
        
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
    }
}
