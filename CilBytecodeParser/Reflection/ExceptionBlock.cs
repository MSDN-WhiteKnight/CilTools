/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    /// <summary>
    /// Represents an exception handling block
    /// </summary>
    public struct ExceptionBlock
    {
        Type _CatchType;
        int _FilterOffset;
        ExceptionHandlingClauseOptions _Flags;
        int _HandlerLength;
        int _HandlerOffset;
        int _TryLength;
        int _TryOffset;

        /// <summary>
        /// Initializes a new ExceptionBlock instance using provided property values
        /// </summary>        
        public ExceptionBlock(
            ExceptionHandlingClauseOptions pFlags,
            int pTryOffset,
            int pTryLength,            
            Type pCatchType,
            int pHandlerOffset,
            int pHandlerLength,            
            int pFilterOffset
            )
        {
            _CatchType = pCatchType;
            _FilterOffset = pFilterOffset;
            _Flags = pFlags;
            _HandlerLength = pHandlerLength;
            _HandlerOffset = pHandlerOffset;
            _TryLength = pTryLength;
            _TryOffset = pTryOffset;
        }

        /// <summary>
        /// Gets the type of exception handled by the catch block
        /// </summary>
        public Type CatchType { get { return this._CatchType; } }

        /// <summary>
        /// Gets the offset of this block's exception filter within the method body, in bytes
        /// </summary>
        public int FilterOffset { get { return this._FilterOffset; } }

        /// <summary>
        /// Gets the value specifying the type of exception block
        /// </summary>
        public ExceptionHandlingClauseOptions Flags { get { return this._Flags; } }

        /// <summary>
        /// Gets the length of this block's handler, in bytes
        /// </summary>
        public int HandlerLength { get { return this._HandlerLength; } }

        /// <summary>
        /// Gets the offset of this block's handler within the method body, in bytes
        /// </summary>
        public int HandlerOffset { get { return this._HandlerOffset; } }

        /// <summary>
        /// Gets the length of this block's try clause, in bytes
        /// </summary>
        public int TryLength { get { return this._TryLength; } }

        /// <summary>
        /// Gets the offset of this block's try clause within the method body, in bytes
        /// </summary>
        public int TryOffset { get { return this._TryOffset; } }

        /// <summary>
        /// Creates new ExceptionBlock instance based on the specified reflection ExceptionHandlingClause object
        /// </summary>        
        public static ExceptionBlock FromReflection(ExceptionHandlingClause clause)
        {
            int filter = 0;
            Type t = null;

            if ((clause.Flags & ExceptionHandlingClauseOptions.Filter) != 0) filter = clause.FilterOffset;

            if(clause.Flags == ExceptionHandlingClauseOptions.Clause) t = clause.CatchType;

            return new ExceptionBlock(
                clause.Flags,
                clause.TryOffset, clause.TryLength,
                t, clause.HandlerOffset, clause.HandlerLength,
                filter
                );
        }
    }
}
