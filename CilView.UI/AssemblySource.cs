/* CIL Tools 
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using CilView.Common;

namespace CilView.UI
{
    abstract class AssemblySource : IDisposable, INotifyPropertyChanged
    {
        static Dictionary<string, Type[]> typecache = new Dictionary<string, Type[]>();
        static readonly object sync = new object();

        protected ObservableCollection<Assembly> assemblies = new ObservableCollection<Assembly>();
        protected ObservableCollection<Type> types = new ObservableCollection<Type>();
        protected ObservableCollection<MethodBase> methods = new ObservableCollection<MethodBase>();

        public static void TypeCacheSetValue(Assembly ass, Type[] types)
        {
            if (types.Length == 0) return;

            string key = ass.Location;
            if (String.IsNullOrEmpty(key)) return;

            lock (sync)
            {
                typecache[key] = types;
            }
        }

        public static Type[] TypeCacheGetValue(Assembly ass)
        {
            string key = ass.Location;
            if (String.IsNullOrEmpty(key)) return null;

            lock (sync)
            {
                if (typecache.ContainsKey(key)) return typecache[key];
                else return null;
            }
        }

        public static void TypeCacheClear()
        {
            lock (sync) { typecache.Clear(); }
        }

        public static ObservableCollection<Type> LoadTypes(Assembly ass)
        {
            Type[] cached = TypeCacheGetValue(ass);

            if (cached != null) return new ObservableCollection<Type>(cached);

            List<Type> ret;

            try
            {
                ret = new List<Type>(ass.GetTypes());

                if (ret != null) TypeCacheSetValue(ass, ret.ToArray());
            }
            catch (ReflectionTypeLoadException e)
            {
                StringBuilder sb = new StringBuilder(500);
                sb.Append(e.GetType().ToString());
                sb.Append(':');
                sb.Append(' ');
                sb.AppendLine(e.Message);
                sb.Append(e.LoaderExceptions.Length.ToString());
                sb.AppendLine(" total errors. First error is:");

                if (e.LoaderExceptions.Length > 0)
                {
                    sb.AppendLine(e.LoaderExceptions[0].GetType() + " - " + e.LoaderExceptions[0].Message);
                }

                Console.WriteLine("Warning: " + sb.ToString());

                ret = new List<Type>();
                for (int i = 0; i < e.Types.Length; i++)
                {
                    if (e.Types[i] != null) ret.Add(e.Types[i]);
                }
            }

            ret.Sort((x, y) => string.Compare(x.ToString(), y.ToString(), StringComparison.InvariantCulture));

            return new ObservableCollection<Type>(ret);
        }

        internal static string MethodToString(MethodBase m)
        {
            ParameterInfo[] pars = m.GetParameters();
            StringBuilder sb = new StringBuilder();
            sb.Append(m.Name);

            if (m.IsGenericMethod)
            {
                sb.Append('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append('>');
            }

            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");
                sb.Append(pars[i].ParameterType.Name);
            }

            sb.Append(')');
            return sb.ToString();
        }

        public static ObservableCollection<MethodBase> LoadMethods(Type t)
        {
            List<MethodBase> ret = new List<MethodBase>();
            MemberInfo[] members = t.GetMembers(Utils.AllMembers | BindingFlags.DeclaredOnly);

            foreach (MemberInfo member in members)
            {
                if (member is MethodBase) ret.Add((MethodBase)member);
            }

            ret.Sort((x, y) =>
            {
                string s1 = string.Empty, s2 = string.Empty;

                try
                {
                    s1 = MethodToString(x);
                    s2 = MethodToString(y);
                }
                catch (TypeLoadException) { }
                catch (System.IO.FileNotFoundException) { }

                return string.Compare(s1, s2, StringComparison.InvariantCulture);
            });

            return new ObservableCollection<MethodBase>(ret);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract bool HasProcessInfo { get; }
        public abstract string GetProcessInfoString();
        public abstract void Dispose();

        public ObservableCollection<Assembly> Assemblies
        {
            get { return this.assemblies; }

            set
            {
                this.assemblies = value;
                OnPropertyChanged("Assemblies");
            }
        }

        public ObservableCollection<Type> Types
        {
            get { return this.types; }

            set
            {
                this.types = value;
                OnPropertyChanged("Types");
            }
        }

        public ObservableCollection<MethodBase> Methods
        {
            get { return this.methods; }

            set
            {
                this.methods = value;
                OnPropertyChanged("Methods");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        public IEnumerable<SearchResult> Search(string text)
        {
            SearchResult res;
            HashSet<int> returned;

            //if type is selected, search methods within that type

            if (methods != null && methods.Count > 0)
            {
                //store already returned indices to prevent duplication
                returned = new HashSet<int>();

                //exact name match
                for (int i = 0; i < methods.Count; i++)
                {
                    if (Utils.StringEquals(methods[i].Name, text))
                    {
                        res = new SearchResult(methods[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }

                //match from beginning
                for (int i = 0; i < methods.Count; i++)
                {
                    if (methods[i].Name.StartsWith(text))
                    {
                        res = new SearchResult(methods[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }

                //match substrings
                for (int i = 0; i < methods.Count; i++)
                {
                    if (methods[i].Name.Contains(text))
                    {
                        res = new SearchResult(methods[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }
            }

            //if assembly is selected, search in types within that assembly

            if (types != null && types.Count > 0)
            {
                //store already returned indices to prevent duplication
                returned = new HashSet<int>();

                //match short name first
                for (int i = 0; i < types.Count; i++)
                {
                    if (Utils.StringEquals(types[i].Name, text))
                    {
                        res = new SearchResult(types[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }

                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i].Name.StartsWith(text))
                    {
                        res = new SearchResult(types[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }

                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i].Name.Contains(text))
                    {
                        res = new SearchResult(types[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }

                //then match full name
                for (int i = 0; i < types.Count; i++)
                {
                    if (Utils.StringEquals(types[i].FullName, text))
                    {
                        res = new SearchResult(types[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }

                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i].FullName.Contains(text))
                    {
                        res = new SearchResult(types[i], i);

                        if (!returned.Contains(i))
                        {
                            returned.Add(i);
                            yield return res;
                        }
                    }
                }
            }

            //search in assemblies
            if (this.assemblies == null) yield break;
            if (this.assemblies.Count <= 1) yield break;

            for (int i = 0; i < assemblies.Count; i++)
            {
                if (assemblies[i].FullName.Contains(text))
                {
                    res = new SearchResult();
                    res.Kind = SearchResultKind.Assembly;
                    res.Index = i;
                    res.Name = "Assembly: " + assemblies[i].FullName;
                    yield return res;
                }
            }
        }
    }
}
