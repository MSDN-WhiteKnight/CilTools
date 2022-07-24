/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Reflection;

namespace CilTools.Internal
{    
    internal class ReflectionInfoContainer : IReflectionInfo
    {
        Dictionary<int, ReflectionInfoElement> items = new Dictionary<int, ReflectionInfoElement>(10);

        public void Add(int id, string name, object val)
        {
            this.items[id] = new ReflectionInfoElement(id, name, val);
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public IEnumerable<string> EnumReflectionProperties()
        {
            foreach (int key in this.items.Keys)
            {
                yield return this.items[key].Name;
            }
        }

        public IEnumerable<int> EnumReflectionPropertyIds()
        {
            foreach (int key in this.items.Keys)
            {
                yield return key;
            }
        }

        public object GetReflectionProperty(string propertyName)
        {
            foreach (int key in this.items.Keys)
            {
                if (this.items[key].Name.Equals(propertyName, StringComparison.Ordinal))
                {
                    return this.items[key].Value;
                }
            }

            return null;
        }

        public object GetReflectionProperty(int id)
        {
            return this.items[id];
        }

        public bool HasReflectionProperty(string propertyName)
        {
            foreach (int key in this.items.Keys)
            {
                if (this.items[key].Name.Equals(propertyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasReflectionProperty(int id)
        {
            return this.items.ContainsKey(id);
        }

        class ReflectionInfoElement
        {
            public ReflectionInfoElement(int id, string name, object val)
            {
                this.Id = id;
                this.Name = name;
                this.Value = val;
            }

            public int Id { get; set; }
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}
