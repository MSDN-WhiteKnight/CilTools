/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    internal static class Utils
    {
        internal static IEnumerable<string> GetInstanceFields(IAddressableTypedEntity obj)
        {
            var fields = obj.Type.Fields;

            for (int i = 0; i < fields.Count; i++)
            {
                yield return fields[i].Name;
            }
        }

        internal static object GetObjectFieldValue(ClrObject obj, string name)
        {
            if (obj.IsNull) return null;
            return GetFieldValue(obj, name);
        }

        internal static object GetFieldValue(IAddressableTypedEntity obj, string name)
        {
            ClrInstanceField f = obj.Type.GetFieldByName(name);

            switch (f.ElementType)
            {
                //primitive types
                case ClrElementType.Int32: return (object)obj.GetField<int>(name);
                case ClrElementType.UInt32: return (object)obj.GetField<uint>(name);
                case ClrElementType.Int8: return (object)obj.GetField<sbyte>(name);
                case ClrElementType.UInt8: return (object)obj.GetField<byte>(name);
                case ClrElementType.Int16: return (object)obj.GetField<short>(name);
                case ClrElementType.UInt16: return (object)obj.GetField<ushort>(name);
                case ClrElementType.Int64: return (object)obj.GetField<long>(name);
                case ClrElementType.UInt64: return (object)obj.GetField<ulong>(name);
                case ClrElementType.Boolean: return (object)obj.GetField<bool>(name);
                case ClrElementType.String: return obj.GetStringField(name);

                //IntPtr
                case ClrElementType.NativeInt:
                    try
                    {
                        long i = obj.GetField<long>(name);
                        IntPtr ptr = new IntPtr(i);
                        return (object)ptr;
                    }
                    catch (InvalidCastException)
                    {
                        return (object)f.ElementType;
                    }

                //class
                case ClrElementType.Class:
                case ClrElementType.Object:
                    ClrObject ret = obj.GetObjectField(name);
                    if (ret.IsNull) return null;
                    return ret;

                //struct
                case ClrElementType.Struct:
                    ClrValueClass cvc = obj.GetValueClassField(name);
                    return cvc;

                //other
                default: return (object)f.ElementType;
            }
        }

        internal static string DumpObject(IAddressableTypedEntity obj)
        {
            StringBuilder sb = new StringBuilder(500);

            foreach (string f in GetInstanceFields(obj))
            {
                sb.Append(f);
                sb.Append(':');
                sb.Append(' ');

                object val = GetFieldValue(obj, f);

                if (val == null)
                {
                    sb.AppendLine("(null)");
                    continue;
                }

                if (val is ClrObject)
                {
                    ClrType t = ((ClrObject)val).Type;
                    sb.Append("(object) ");
                    sb.Append('[');
                    sb.Append(t.Name);
                    sb.Append(']');
                }
                else if (val is ClrValueClass)
                {
                    ClrType t = ((ClrValueClass)val).Type;
                    sb.Append("(struct) ");
                    sb.Append('[');
                    sb.Append(t.Name);
                    sb.Append(']');
                }
                else if (val is ClrElementType)
                {
                    sb.Append('[');
                    sb.Append(val.ToString());
                    sb.Append(']');
                }
                else
                {
                    sb.Append(val.ToString());
                    sb.Append(' ');
                    sb.Append('[');
                    sb.Append(val.GetType().ToString());
                    sb.Append(']');
                }

                sb.AppendLine();
            }//end foreach

            return sb.ToString();
        }
    }
}
