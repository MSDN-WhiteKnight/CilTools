/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.SourceCode;

namespace CilView.SourceCode
{
    class PdbCodeProvider : ICodeProvider
    {
        public IEnumerable<SourceDocument> GetSourceCodeDocuments(MethodBase m)
        {
            throw new NotImplementedException();
        }
    }
}
