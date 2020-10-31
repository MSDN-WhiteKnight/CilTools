﻿/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilView
{
    enum SearchResultKind
    {
        Assembly=1,
        Type=2,
        Method=3
    }

    class SearchResult
    {
        public SearchResult() { }

        public SearchResult(Type t, int i)
        {
            this.Kind = SearchResultKind.Type;
            this.Index = i;
            this.Name = t.FullName;
            this.Value = t;
        }

        public SearchResult(MethodBase m, int i)
        {
            this.Kind = SearchResultKind.Method;
            this.Index = i;
            this.Name = CilVisualization.MethodToString(m);
            this.Value = m;
        }

        public SearchResultKind Kind {get;set;}
        public int Index { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
