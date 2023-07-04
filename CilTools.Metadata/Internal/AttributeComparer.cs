/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Reflection;

namespace CilTools.Internal
{
    /// <summary>
    /// Compares custom attributes by types. Only works for <see cref="ICustomAttribute"/> implementations.
    /// </summary>
    class AttributeComparer : IEqualityComparer<object>
    {
        private AttributeComparer() { }

        internal static readonly AttributeComparer Value = new AttributeComparer();

        public new bool Equals(object x, object y)
        {
            if (x == null)
            {
                if (y == null) return true;
                else return false;
            }

            if (y == null) return false;

            if (!(x is ICustomAttribute) || !(y is ICustomAttribute)) return false;

            ICustomAttribute left = (ICustomAttribute)x;
            ICustomAttribute right = (ICustomAttribute)y;

            if (left.Constructor == null || right.Constructor == null) return false;

            if (left.Constructor.DeclaringType == null || right.Constructor.DeclaringType == null) return false;

            return Utils.TypeEquals(left.Constructor.DeclaringType, right.Constructor.DeclaringType);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null) return 0;

            if (!(obj is ICustomAttribute)) obj.GetHashCode();

            ICustomAttribute attr = (ICustomAttribute)obj;

            if (attr.Constructor == null) return obj.GetHashCode();

            if (attr.Constructor.DeclaringType == null) return obj.GetHashCode();

            if (attr.Constructor.DeclaringType.FullName == null) return obj.GetHashCode();
            else return attr.Constructor.DeclaringType.FullName.GetHashCode();
        }
    }
}
