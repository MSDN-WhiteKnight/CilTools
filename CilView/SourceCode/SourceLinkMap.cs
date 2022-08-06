/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace CilView.SourceCode
{
    /// <summary>
    /// Represents information that helps locating source code on remote server.
    /// Source link specification: 
    /// https://github.com/dotnet/designs/blob/main/accepted/2020/diagnostics/source-link.md#source-link-json-schema
    /// </summary>
    public class SourceLinkMap
    {
        SourceLinkEntry[] entries;

        static readonly SourceLinkMap Empty = new SourceLinkMap(new SourceLinkEntry[0]);

        SourceLinkMap(SourceLinkEntry[] e)
        {
            this.entries = e;
        }
        
        /// <summary>
        /// Converts local path stored in symbols into the source link server URL
        /// </summary>
        public string GetServerPath(string symbolsPath)
        {
            for (int i = 0; i < this.entries.Length; i++)
            {
                string serverpath = this.entries[i].GetServerPath(symbolsPath);

                if (serverpath.Length > 0) return serverpath;
            }

            return string.Empty;
        }

        public static SourceLinkMap Read(string json)
        {
            JObject j = JObject.Parse(json);
            JObject docs = j["documents"] as JObject;

            if (docs == null) return Empty;

            List<SourceLinkEntry> ret = new List<SourceLinkEntry>();
            
            foreach (JProperty prop in docs.Properties())
            {
                SourceLinkEntry entry = new SourceLinkEntry();
                entry.SymbolsPath = prop.Name;
                entry.ServerPath = docs[prop.Name].Value<string>();

                //validate early, so path mapping won't blow up with cryptic exceptions on incorrect data

                if (entry.ServerPath == null)
                {
                    throw new Exception("Invalid source link data: server path is null");
                }

                if (entry.ServerPath.Length <= 1)
                {
                    throw new Exception("Invalid source link data: server path is too short");
                }

                ret.Add(entry);
            }

            return new SourceLinkMap(ret.ToArray());
        }
    }
}
