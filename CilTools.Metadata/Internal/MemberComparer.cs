/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Internal
{
    /// <summary>
    /// Compares members by value to deduplicate inherited members
    /// </summary>
    internal class MemberComparer : IEqualityComparer<MemberInfo>
    {
        private MemberComparer() { }

        static MemberComparer instance;

        public static MemberComparer Instance
        {
            get
            {
                if (instance == null) instance = new MemberComparer();

                return instance;
            }
        }

        public bool Equals(MemberInfo x, MemberInfo y)
        {
            if (x == null)
            {
                if (y != null) return false;
                else return true;
            }
            else if (y == null) return false;

            if (x is MethodBase && y is MethodBase)
            {
                //methods are compared by name and signature
                MethodBase mb1 = (MethodBase)x;
                MethodBase mb2 = (MethodBase)y;

                if (!Utils.StrEquals(mb1.Name, mb2.Name)) return false;
                if (mb1.IsStatic != mb2.IsStatic) return false;

                Type[] sig = Utils.GetParameterTypesArray(mb1);
                ParameterInfo[] pars = mb2.GetParameters();
                return Utils.ParamsMatchSignature(pars, sig);
            }
            else
            {
                //others are compared by name
                return x.MemberType == y.MemberType && Utils.StrEquals(x.Name, y.Name);
            }
        }

        public int GetHashCode(MemberInfo obj)
        {
            string name = obj.Name;

            if (name != null) return name.GetHashCode();
            else return obj.GetHashCode();
        }
    }
}
