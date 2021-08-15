/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace CilView.Build
{
    class ProjectInfo
    {
        public bool IsSDK { get; set; }
        public string Name { get; set; }
        public string ProjectPath { get; set; }
        public string OutputType { get; set; }
        public string[] TargetFrameworks { get; set; }

        public ProjectInfo()
        {
            this.Name = String.Empty;
            this.OutputType = String.Empty;
            this.TargetFrameworks = new string[0];
        }

        public static ProjectInfo ReadFile(string projectPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(projectPath);
            ProjectInfo ret = new ProjectInfo();
            ret.ProjectPath = projectPath;

            if (doc.DocumentElement.Name.Equals("Project", StringComparison.Ordinal))
            {
                XmlAttribute sdk=doc.DocumentElement.Attributes["Sdk"];

                if(sdk!=null && !String.IsNullOrEmpty(sdk.Value))
                { 
                    ret.IsSDK = true; 
                }

                ret.Name = Path.GetFileNameWithoutExtension(projectPath);

                //output type
                string outputType = "Library";
                XmlNodeList nodes=doc.GetElementsByTagName("OutputType");

                if (nodes.Count > 0)
                {
                    if (!String.IsNullOrEmpty(nodes[0].InnerText))
                    {
                        outputType = nodes[0].InnerText;
                    }
                }

                ret.OutputType = outputType;

                //target frameworks
                
                if (ret.IsSDK)
                {
                    nodes = doc.GetElementsByTagName("TargetFramework");

                    if (nodes.Count > 0)
                    {
                        if (!String.IsNullOrEmpty(nodes[0].InnerText))
                        {
                            //single TFM
                            ret.TargetFrameworks=new string[] { nodes[0].InnerText };
                        }
                    }
                    else
                    {
                        nodes = doc.GetElementsByTagName("TargetFrameworks");
                        string val = String.Empty;

                        if (nodes.Count > 0 && !String.IsNullOrEmpty(nodes[0].InnerText))
                        {
                            //multi-targeting
                            val = nodes[0].InnerText;

                            string[] arr = val.Split(
                                new char[] { ';' },
                                StringSplitOptions.RemoveEmptyEntries
                                );

                            ret.TargetFrameworks = arr;
                        }
                    }
                }//end if (ret.IsSDK)

                return ret;
            }
            else
            {
                throw new NotSupportedException(
                    "Project format is not supported. Unexpected root element: "+ 
                    doc.DocumentElement.Name
                    );
            }
        }
    }
}
