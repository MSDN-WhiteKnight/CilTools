/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilView.Build
{
    static class ProjectGenerator
    {
        public static string CreateProject(string codeFile) 
        {
            string name = Path.GetFileNameWithoutExtension(codeFile);
            string ext = Path.GetExtension(codeFile);
            string proj_ext;

            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                proj_ext = ".csproj";
            }
            else
            {
                throw new NotSupportedException("Unknown programming language: " + ext);
            }
            
            string dir = BuildSystemInvocation.CreateTempDir(name, "proj");
            BuildSystemInvocation.RegisterTempDir(dir);

            //load project template from resources
            string template;
            Assembly ass = typeof(ProjectGenerator).Assembly;

            using (Stream stream = ass.GetManifestResourceStream("CilView.Build.CsTemplate.xml"))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = reader.ReadToEnd();
            }

            //write project file
            string proj_path = Path.Combine(dir, name + proj_ext);
            File.WriteAllText(proj_path, template);

            //write code file
            string out_path= Path.Combine(dir, "Class1.cs");
            File.Copy(codeFile, out_path);

            return proj_path;
        }
    }
}
