/* CIL Tools 
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using CilTools.Visualization;
using Html.Forms;

namespace CilView.UI
{
    public static class AppBuilder
    {
        public static Application BuildApplication(string assemblyPath)
        {
            Application app = new Application();
            app.Name = "CIL View";
            IndexPage page = new IndexPage();
            page.AssemblyPath = assemblyPath;
            app.AddPage(page);

            string styles = HtmlVisualizer.GetVisualStyles();
            ContentData asset = ContentData.FromString(styles, ContentTypes.CSS);
            asset.FileName = "styles.css";
            app.AddAsset(asset);

            return app;
        }
    }
}
