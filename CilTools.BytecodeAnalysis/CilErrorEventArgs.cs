/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents data associated with Error event
    /// </summary>
    public class CilErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Exception associated with this event
        /// </summary>
        protected Exception _Exception;

        /// <summary>
        /// Additional information associated with this event
        /// </summary>
        protected string _Info;

        /// <summary>
        /// A date and time when this event occured
        /// </summary>
        protected DateTime _Timestamp;

        /// <summary>
        /// Gets exception associated with this event
        /// </summary>
        public Exception Exception { get { return this._Exception; } }

        /// <summary>
        /// Gets additional information associated with this event
        /// </summary>
        public string Info { get { return this._Info; } }

        /// <summary>
        /// Gets date and time when this event occured
        /// </summary>
        public DateTime Timestamp { get { return this._Timestamp; } }

        /// <summary>
        /// Creates new CilErrorEventArgs object with specified Exception and error information
        /// </summary>
        /// <param name="ex">Exception associated with this event</param>
        /// <param name="info">Additional information associated with this event</param>
        public CilErrorEventArgs(Exception ex, string info)
        {
            this._Exception = ex;
            this._Info = info;
            this._Timestamp = DateTime.Now;
        }
    }
}
