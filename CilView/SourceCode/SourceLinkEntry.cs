/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.SourceCode
{
    public class SourceLinkEntry
    {
        public string SymbolsPath { get; set; }
        public string ServerPath { get; set; }

        public string GetServerPath(string symbolsPath)
        {
            if (this.SymbolsPath.EndsWith("*", StringComparison.Ordinal))
            {
                //wildcard
                string matchPrefix = this.SymbolsPath.Substring(0, this.SymbolsPath.Length - 1);

                if (symbolsPath.StartsWith(matchPrefix, StringComparison.Ordinal))
                {
                    if(matchPrefix.Length >= symbolsPath.Length) return string.Empty;

                    string symbolsPathSuffix = symbolsPath.Substring(matchPrefix.Length);
                    string mappedPath = this.ServerPath.Replace("*", symbolsPathSuffix);
                    mappedPath = mappedPath.Replace("\\", "/");
                    return mappedPath;
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                //exact
                if (this.SymbolsPath.Equals(symbolsPath, StringComparison.Ordinal))
                {
                    return this.ServerPath;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
