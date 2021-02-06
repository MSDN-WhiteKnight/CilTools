/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilView.Exceptions
{
    public class TypeExceptionInfo
    {
        string typename;
        Dictionary<string, string[]> exceptions;

        public TypeExceptionInfo(string tn, Dictionary<string, string[]> exc)
        {
            this.typename = tn;
            this.exceptions = exc;
        }

        public string TypeName { get { return this.typename; } }

        public string[] GetExceptions(string method)
        {
            string[] arr = exceptions[method];
            if (arr == null) return null;

            string[] ret = new string[arr.Length];
            Array.Copy(arr, ret, arr.Length);
            return ret;
        }

        public IEnumerable<string> Methods
        {
            get
            {
                foreach (string key in exceptions.Keys) yield return key;
            }
        }

        public static TypeExceptionInfo GetFromXML(string file)
        {
            Dictionary<string, string[]> ret = new Dictionary<string, string[]>();

            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlNode members = doc.FirstChild["Members"];
            string typename = doc.FirstChild.Attributes["FullName"].Value;

            foreach (XmlNode node in members.ChildNodes)
            {
                if (!(node.NodeType == XmlNodeType.Element)) continue;
                if (!(node.Name == "Member")) continue;
                if (node["MemberType"].InnerText != "Method") continue;

                string mname = node.Attributes["MemberName"].Value;

                //signature
                string docid = "";
                foreach (XmlNode x in node.ChildNodes)
                {
                    if (x.Name == "MemberSignature" && x.Attributes["Language"].Value == "DocId")
                    {
                        docid = x.Attributes["Value"].Value;
                        break;
                    }
                }

                if (String.IsNullOrEmpty(docid)) docid = mname;

                //exceptions
                XmlNode docs = node["Docs"];
                if (docs == null) continue;
                List<string> exceptions = new List<string>(10);

                foreach (XmlNode x in docs.ChildNodes)
                {
                    if (x.Name == "exception")
                    {
                        exceptions.Add(x.Attributes["cref"].Value);
                    }
                }

                ret[docid] = exceptions.ToArray();
            }//end foreach

            return new TypeExceptionInfo(typename,ret);
        }

        public static TypeExceptionInfo GetFromType(Type t)
        {
            Dictionary<string, string[]> ret = new Dictionary<string, string[]>();

            MemberInfo[] members = t.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                );

            for (int i = 0; i < members.Length; i++)
            {
                MethodBase mb = members[i] as MethodBase;
                if (mb == null) continue;
                if (mb.IsConstructor) continue;

                List<string> list = new List<string>(10);
                IEnumerable<ExceptionInfo> exceptions = ExceptionInfo.GetExceptions(mb);

                //signature
                StringBuilder sb = new StringBuilder(100);
                sb.Append("M:");
                if (mb.DeclaringType != null) sb.Append(mb.DeclaringType.FullName);
                sb.Append('.');
                sb.Append(mb.Name);
                ParameterInfo[] pars = mb.GetParameters();

                if (pars.Length > 0)
                {
                    sb.Append('(');

                    for (int j = 0; j < pars.Length; j++)
                    {
                        if (j >= 1) sb.Append(',');
                        sb.Append(pars[j].ParameterType.FullName);
                    }

                    sb.Append(')');
                }

                string sig = sb.ToString();

                //exceptions
                foreach (ExceptionInfo ei in exceptions)
                {
                    list.Add("T:" + ei.ExceptionType.FullName);
                }

                ret[sig] = list.ToArray();
            }

            return new TypeExceptionInfo(t.FullName,ret);
        }
    }
}
