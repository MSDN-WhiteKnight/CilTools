﻿/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;
using CilTools.SourceCode.Common;
using CilTools.Visualization;

namespace CilView.Tests.Visualization
{
    [TestClass]
    public class HtmlVisualizerTests_Source
    {
        [TestMethod]
        public void Test_RenderSourceText()
        {
            string sourceText = @"using System;

public class Program
{
    public static void Main() 
    {
        Console.WriteLine(""Hello, world!"");
    }
}";

            string expected = @"<pre style=""white-space: pre-wrap;""><code><span style=""color: blue;"">using </span>System;

<span style=""color: blue;"">public </span><span style=""color: blue;"">class </span>Program
{
    <span style=""color: blue;"">public </span><span style=""color: blue;"">static </span><span style=""color: blue;"">void </span>Main() 
    {
        Console.WriteLine(<span style=""color: red;"">&quot;Hello, world!&quot;</span>);
    }
}</code></pre>";
            
            //convert source text into tokens
            SyntaxNodeCollection coll = SourceParser.Parse(sourceText, ".cs");

            //convert tokens to HTML
            HtmlVisualizer vis = new HtmlVisualizer();
            string html = vis.RenderToString(coll.GetChildNodes());
            
            //validate results
            AssertThat.MarkupEquals(expected, html);
        }

        [TestMethod]
        public void Test_RenderSourceText_Cpp()
        {
            const string sourceText = @"#include <stdio.h>
int main(int argc, char* argv[])
{
    printf(""Hello, world!"");
    getchar();
    return 0;
}";

            string expected = @"<pre style=""white-space: pre-wrap;""><code>#include &lt;stdio.h&gt;
<span style=""color: blue;"">int </span>main(<span style=""color: blue;"">int </span>argc, <span style=""color: blue;"">char</span>* argv[])
{
    printf(<span style=""color: red;"">&quot;Hello, world!&quot;</span>);
    getchar();
    <span style=""color: blue;"">return </span>0;
}</code></pre>";

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);

            //convert source text into tokens
            SyntaxNodeCollection coll = SourceParser.Parse(sourceText, ".cpp");

            //convert tokens to HTML
            HtmlVisualizer vis = new HtmlVisualizer();
            vis.RenderNodes(coll.GetChildNodes(), new VisualizationOptions(), wr);
            
            //validate output
            string html = sb.ToString().Trim();
            expected = expected.Trim();
            AssertThat.MarkupEquals(expected, html);
        }
    }
}
