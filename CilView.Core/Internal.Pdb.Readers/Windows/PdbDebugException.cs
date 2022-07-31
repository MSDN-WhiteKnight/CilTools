// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file for more information.

using System.IO;

namespace Internal.Pdb.Windows
{
    internal class PdbDebugException : IOException
    {
        internal PdbDebugException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }
    }
}