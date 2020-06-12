﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace CilView
{
    abstract class AssemblySource:IDisposable,INotifyPropertyChanged 
    {
        public static ObservableCollection<Type> LoadTypes(Assembly ass)
        {
            List<Type> ret;
            
            try
            {
                ret = new List<Type>(ass.GetTypes());
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
                
                MessageBox.Show(sb.ToString(), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                ret = new List<Type>();
                for (int i = 0; i < e.Types.Length; i++)
                {
                    if(e.Types[i]!=null) ret.Add(e.Types[i]);
                }
            }
            
            ret.Sort((x, y) => String.Compare( x.ToString(), y.ToString(), StringComparison.InvariantCulture ));

            return new ObservableCollection<Type>(ret);
        }

        public static ObservableCollection<MethodBase> LoadMethods(Type t)
        {
            List<MethodBase> ret = new List<MethodBase>();
            MemberInfo[] members = t.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );

            foreach (MemberInfo member in members)
            {
                if (member is MethodBase) ret.Add((MethodBase)member);
            }

            ret.Sort((x, y) =>
            {
                string s1="",s2="";

                try
                {
                    s1 = CilVisualization.MethodToString(x);
                    s2 = CilVisualization.MethodToString(y);
                }
                catch (TypeLoadException) { }
                catch (System.IO.FileNotFoundException) { }

                return String.Compare(s1,s2,StringComparison.InvariantCulture);
            });

            return new ObservableCollection<MethodBase>(ret);
        }

        protected ObservableCollection<Assembly> assemblies = new ObservableCollection<Assembly>();
        protected ObservableCollection<Type> types = new ObservableCollection<Type>();
        protected ObservableCollection<MethodBase> methods = new ObservableCollection<MethodBase>();

        public event PropertyChangedEventHandler PropertyChanged;
        
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

            if(handler!=null) handler(this,new PropertyChangedEventArgs(name));
        }
    }
}
