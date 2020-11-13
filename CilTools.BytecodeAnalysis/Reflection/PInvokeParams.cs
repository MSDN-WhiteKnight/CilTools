/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents the parameters of the imported unmanaged method 
    /// </summary>
    /// <remarks>
    /// <para>This type is used with the <see cref="CustomMethod.GetPInvokeParams"/> method.</para>
    /// <para>Imported unmanaged method is a method implemented as a Platform Invoke (P/Invoke) call. 
    /// The call to such method is marshalled into the call to corresponding native function by 
    /// Common Language Runtime. P/Invoke method is defined in .NET using <see cref="DllImportAttribute"/>. 
    /// See the .NET documentation for more information about P/Invoke: 
    /// <see href="https://docs.microsoft.com/dotnet/standard/native-interop/pinvoke"/>.
    /// </para>
    /// </remarks>
    public class PInvokeParams
    {
        string _module;
        string _funcname;
        CharSet _charset;
        bool _exactspell;
        bool _setlasterr;
        bool? _bestfit;
        System.Runtime.InteropServices.CallingConvention _callconv;

        /// <summary>
        /// Creates the new instance of <c>PInvokeParams</c> object.
        /// </summary>
        /// <param name="module">Unmanaged module from which the method is imported</param>
        /// <param name="func">The name of the imported native function</param>
        /// <param name="charSet">The character set used by this method</param>
        /// <param name="exactSpelling">
        /// The value indicating that the marshaller should not probe for charset-specific names 
        /// when searching for an entry point. 
        /// </param>
        /// <param name="setLastError">
        /// The value indicating that the imported function sets the last WinAPI error.
        /// </param>
        /// <param name="bestFitMapping">
        /// The value indicating whether the marshaller should use the best fit mapping behaviour when 
        /// converting characters between character sets.
        /// </param>
        /// <param name="callConv">The clling convention of the imported function.</param>
        public PInvokeParams(
            string module,
            string func,
            CharSet charSet, 
            bool exactSpelling, 
            bool setLastError,
            bool? bestFitMapping,
            System.Runtime.InteropServices.CallingConvention callConv
            )
        {
            this._module = module;
            this._funcname = func;
            this._charset = charSet;
            this._exactspell = exactSpelling;
            this._setlasterr = setLastError;
            this._callconv = callConv;
            this._bestfit = bestFitMapping;
        }

        /// <summary>
        /// Gets the name of the unmanaged module from which the method is imported
        /// </summary>
        public string ModuleName { get { return this._module; } }

        /// <summary>
        /// Gets the name of the imported native function
        /// </summary>
        public string FunctionName { get { return this._funcname; } }

        /// <summary>
        /// Gets the character set used by this method
        /// </summary>
        public CharSet CharSet { get { return this._charset; } }

        /// <summary>
        /// Gets the value indicating that the marshaller should not probe for charset-specific names 
        /// when searching for an entry point.
        /// </summary>
        public bool ExactSpelling { get { return this._exactspell; } }

        /// <summary>
        /// Gets the value indicating that the imported function sets the last WinAPI error.
        /// </summary>
        public bool SetLastError { get { return this._setlasterr; } }

        /// <summary>
        /// Gets the value indicating whether the marshaller should use the best fit mapping behaviour when 
        /// converting characters between character sets.
        /// </summary>
        public bool? BestFitMapping { get { return this._bestfit; } }

        /// <summary>
        /// Gets the calling convention for the imported function.
        /// </summary>
        /// <remarks>
        /// Calling convention is a set of rules defining how the function interacts with its caller. See 
        /// <see href="https://docs.microsoft.com/en-us/cpp/cpp/argument-passing-and-naming-conventions"/> 
        /// for more information about native calling conventions.
        /// </remarks>
        public System.Runtime.InteropServices.CallingConvention CallingConvention 
        { 
            get { return this._callconv; } 
        }
    }
}
