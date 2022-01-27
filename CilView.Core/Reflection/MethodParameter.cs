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

        public MethodParameter(string name, Type t, string initialValue)
        {
            this.Name = name;
            this.ParamType = t;

            if (initialValue != null) this.val = initialValue;
            else this.IsNull = true;
        }

        public string Name { get; set; }

        public Type ParamType { get; set; }

        public string Value
        {
            get => val;

            set
            {
                value = val;
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
