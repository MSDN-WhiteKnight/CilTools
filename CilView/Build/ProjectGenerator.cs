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
    public enum CodeLanguage 
    {
        CSharp=1,VB=2
    }

    class LangItem 
    {
        public CodeLanguage Language { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.Name)) return this.Name;
            else return base.ToString();
        }
    }

    static class ProjectGenerator
    {
        public static string CreateProject(string codeFile) 
        {
            string name = Path.GetFileNameWithoutExtension(codeFile);
            string ext = Path.GetExtension(codeFile);
            CodeLanguage lang;

            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                lang = CodeLanguage.CSharp;
            }
            else
            {
                throw new NotSupportedException("Unknown programming language: " + ext);
            }

            string code = File.ReadAllText(codeFile);

            return CreateProjectFromCode(code, lang, name);
        }

        public static string CreateProjectFromCode(string code,CodeLanguage lang,string name)
        {
            string proj_ext;

            if (lang==CodeLanguage.CSharp)
            {
                proj_ext = ".csproj";
            }
            else
            {
                throw new NotSupportedException("Unknown programming language: " + lang.ToString());
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
            string out_path = Path.Combine(dir, "Class1.cs");
            File.WriteAllText(out_path, code);

            return proj_path;
        }
    }
}
