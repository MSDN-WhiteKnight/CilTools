﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CilView
{
    abstract class AssemblySource:IDisposable,INotifyPropertyChanged 
    {
        public static ObservableCollection<Type> LoadTypes(Assembly ass)
        {
            List<Type> ret = new List<Type>(ass.GetTypes());

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

            ret.Sort((x, y) => String.Compare(
                 CilVisualization.MethodToString(x),
                 CilVisualization.MethodToString(y),
                 StringComparison.InvariantCulture
                ));

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
