/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Visualization
{
    /// <summary>
    /// Specifies visualization output format
    /// </summary>
    public enum OutputFormat
    {
        /// <summary>
        /// Hypertext Markup Language (HTML) format
        /// </summary>
        Html = 1, 

        /// <summary>
        /// Plain text format
        /// </summary>
        Plaintext = 2,
        
        /// <summary>
        /// Text printed into console (with colors)
        /// </summary>
        ConsoleText = 3
    }
}
