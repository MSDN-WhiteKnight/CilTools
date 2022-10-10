/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CilView.Core.Documentation
{
    public static class DocsRewriter
    {
        static string StripImages(string input)
        {
            string ret = input;
            int n_iterations = 0;

            while (true)
            {
                int startIndex = ret.IndexOf("<img");

                if (startIndex < 0) break;

                int endIndex = ret.IndexOf('>', startIndex);

                if (endIndex < 0) break;

                endIndex++;
                string substr = ret.Substring(startIndex, endIndex - startIndex);
                ret = ret.Replace(substr, string.Empty);

                n_iterations++;

                if (n_iterations > 10000)
                {
                    Console.WriteLine("Too many iterations in DocsRewriter.StripImages!");
                    break;
                }
            }

            return ret;
        }

        public static void ExtractArticle(string inputPath, string outputPath)
        {
            string text = File.ReadAllText(inputPath);
            const string start = "<article class=\"content wrap\" id=\"_content\" data-uid=\"\">";
            const string end = "</article>";

            const string footer = "<p><a href=\"https://msdn-whiteknight.github.io/CilTools/articles/cilview-manual.html\">" +
                "View latest version of this document online</a></p>";

            int startIndex = text.IndexOf(start);
            int endIndex = text.IndexOf(end);

            if (startIndex < 0) throw new Exception("Article start not found");
            if (endIndex < 0) throw new Exception("Article end not found");

            startIndex += start.Length;
            string article = text.Substring(startIndex, endIndex - startIndex);
            article = StripImages(article);
            StreamWriter output = new StreamWriter(outputPath);

            using (output)
            {
                output.WriteLine("<html><head><title>CIL View user manual</title></head><body>");
                output.WriteLine(article);
                output.WriteLine(footer);
                output.WriteLine("</body></html>");
            }
        }
    }
}
