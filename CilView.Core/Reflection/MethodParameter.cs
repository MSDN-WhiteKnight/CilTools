/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core.Reflection
{
    public class MethodParameter : INotifyPropertyChanged
    {
        string val;
        private bool isNull;
        string name;
        Type paramType;

        public MethodParameter(string name, Type t, string initialValue)
        {
            this.name = name;
            this.paramType = t;

            if (initialValue != null) this.val = initialValue;
            else this.IsNull = true;
        }

        public string Name { get => name; }

        public Type ParamType { get => paramType; }

        public string Value
        {
            get => val;

            set
            {
                val = value;
                OnPropertyChanged("Value");
            }
        }

        public bool IsNull
        {
            get => isNull;

            set
            {
                isNull = value;
                OnPropertyChanged("IsNull");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
