/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System.Collections.Generic;
using System.Reflection;

namespace CilTools.SourceCode
{
    /// <summary>
    /// Represents an object that loads source code
    /// </summary>
    public abstract class SourceCodeProvider
    {
        /// <summary>
        /// Gets a collection of documents that contain the source code of the specified method
        /// </summary>
        /// <param name="m">Method which source code to get</param>
        public abstract IEnumerable<SourceDocument> GetSourceCodeDocuments(MethodBase m);

        /// <summary>
        /// Gets a string that contains the source code of the specified method's signature
        /// </summary>
        /// <param name="m">Method which signature to get</param>
        public abstract string GetSignatureSourceCode(MethodBase m);
    }
}
