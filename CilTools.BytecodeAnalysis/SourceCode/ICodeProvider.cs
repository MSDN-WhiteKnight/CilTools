/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System.Collections.Generic;
using System.Reflection;

namespace CilTools.SourceCode
{
    public interface ICodeProvider
    {
        IEnumerable<SourceDocument> GetSourceCodeDocuments(MethodBase m);
    }
}
