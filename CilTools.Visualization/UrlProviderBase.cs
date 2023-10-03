/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Visualization
{
    /// <summary>
    /// A base class for classes that return URL for a given member. Deriving from this class is useful for custom 
    /// navigation systems.
    /// </summary>
    public abstract class UrlProviderBase
    {
        /// <summary>
        /// Gets the URL of the specified member
        /// </summary>
        /// <param name="member">Member to get URL</param>
        /// <returns>
        /// String containing requested URL, or an empty string if this provider can't return URL for this member
        /// </returns>
        public abstract string GetMemberUrl(MemberInfo member);
    }
}
