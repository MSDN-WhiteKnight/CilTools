/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Runtime.Methods
{
    internal class AttributesCollection : ICustomAttributeProvider
    {
        object[] collItems;

        public static readonly AttributesCollection Empty = new AttributesCollection(new object[0]);

        public AttributesCollection(object[] items)
        {
            this.collItems = items;
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            return this.collItems;
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
