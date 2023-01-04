/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.Common
{
    public static class SourceTokenReader
    {
        public static SourceToken[] ReadAllTokens(string src, IEnumerable<SyntaxTokenDefinition> definitions, 
            TokenClassifier classifier)
        {
            SourceTokenFactory factory = new SourceTokenFactory(classifier);
            return SyntaxReader.ReadAllNodes(src, definitions, factory).Cast<SourceToken>().ToArray();
        }
    }
}
