/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Metadata;
using CilTools.Tests.Common;
using CilTools.Visualization;

namespace CilView.Tests.Visualization
{
    [TestClass]
    public class CilVisualizerTests
    {
        [TestMethod]
        public void Test_RenderMethod()
        {
            HtmlVisualizer vis = new HtmlVisualizer();
            AssemblyReader reader = new AssemblyReader();
            string str;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(typeof(SampleType).FullName);
                MethodBase mb = t.GetMethod("CalcSum");
                str = HtmlVisualization.RenderMethod(mb, vis, new VisualizationOptions());
            }

            Assert.IsTrue(str.Contains("<span>          ldfld        </span>" +
                "<span style=\"color: blue;\">float32</span> <span class=\"memberid\">CilView.Tests.Visualization</span>." +
                "<span class=\"memberid\">SampleType</span>::<span class=\"memberid\">X</span>"));

            Assert.IsTrue(str.Contains("<span>          ldfld        </span>" +
                "<span style=\"color: blue;\">float32</span> <span class=\"memberid\">CilView.Tests.Visualization</span>." +
                "<span class=\"memberid\">SampleType</span>::<span class=\"memberid\">Y</span>"));

            Assert.IsTrue(str.Contains("<span>          add          </span>"));
            Assert.IsTrue(str.Contains("<span>          ret          </span>"));
        }

        [TestMethod]
        public void Test_RenderType()
        {
            string expected = @"<pre style=""white-space: pre-wrap;""><code><span style=""color: magenta;"">.class </span><span style=""color: blue;"">public </span><span style=""color: blue;"">auto </span><span style=""color: blue;"">ansi </span><span style=""color: blue;"">beforefieldinit </span><span class=""memberid"">CilView.Tests.Visualization.SampleType</span>
<span style=""color: blue;"">extends </span>[<span>System.Runtime</span>]<span class=""memberid"">System</span>.<span class=""memberid"">Object</span>
{

<span style=""color: magenta;""> .field </span><span style=""color: blue;"">public </span><span style=""color: blue;"">float32</span><span class=""memberid""> X</span>
<span style=""color: magenta;""> .field </span><span style=""color: blue;"">public </span><span style=""color: blue;"">float32</span><span class=""memberid""> Y</span>

<span style=""color: green;""> //...
</span>
}
</code></pre>";

            HtmlVisualizer vis = new HtmlVisualizer();
            AssemblyReader reader = new AssemblyReader();
            string html;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(typeof(SampleType).FullName);
                html = HtmlVisualization.RenderType(t, vis, full: false);
            }

            AssertThat.MarkupEquals(expected, html);
        }

        [TestMethod]
        public void Test_RenderAssemblyManifest()
        {
            HtmlVisualizer vis = new HtmlVisualizer();
            AssemblyReader reader = new AssemblyReader();
            string html;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                html = HtmlVisualization.RenderAssemblyManifest(ass, vis);
            }

            AssertThat.MarkupContains(html, "<span style=\"color: magenta;\">.assembly </span>" +
                "<span style=\"color: blue;\">extern </span><span>CilTools.BytecodeAnalysis</span>");

            AssertThat.MarkupContains(html,
                "<span style=\"color: magenta;\">.assembly </span><span>CilView.Tests</span>{");

            AssertThat.MarkupContains(html, "<span style=\"color: magenta;\">  .custom </span>" +
                "<span style=\"color: blue;\">instance </span><span style=\"color: blue;\">void</span> " +
                "[<span>System.Runtime</span>]<span class=\"memberid\">System.Reflection</span>." +
                "<span class=\"memberid\">AssemblyFileVersionAttribute</span>");

            AssertThat.MarkupContains(html,
                "<span style=\"color: magenta;\">.module </span><span>CilView.Tests.dll</span>");
        }
    }
}
