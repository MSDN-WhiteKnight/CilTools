/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents a Portable Executable (PE) image stored in memory
    /// </summary>
    /// <remarks>
    /// This class provides the centalized mechanism for storing and passing PE images loaded into memory within the CIL Tools 
    /// suite. It does not implement any logic for loading and processing of images, this is provided by other APIs. 
    /// The <c>CilTools.Runtime.ClrAssemblyReader.GetMemoryImage</c> method loads the PE image from the address space of
    /// the external .NET process.
    /// The <c>CilTools.Metadata.AssemblyReader.LoadImage</c> method is used to inspect memory images.
    /// </remarks>
    public class MemoryImage
    {
        byte[] _image;
        string _filepath;
        bool _isfile;

        /// <summary>
        /// Creates a new instance of the <c>MemoryImage</c> class using the specified byte array
        /// </summary>
        /// <param name="image">The byte array with PE image contents</param>
        /// <param name="filepath">
        /// The path of the file where this image data was loaded from (could be an empty string)
        /// </param>
        /// <param name="isFileLayout">
        /// The value indicating that this image is a raw PE file data, rather than an image modified 
        /// by the OS loader.
        /// </param>
        /// <remarks>
        /// <para>The <paramref name="filepath"/> parameter is optional and used only to identify this image, for example, 
        /// for caching purposes. You could pass null or an empty string if you don't want to specify it.</para>
        /// <para>Set the <paramref name="isFileLayout"/> parameter to <c>true</c> if this image was loaded by 
        /// directly reading raw data from a file. If the image was obtained by reading the address space of some process 
        /// and it contains PE data modified by operating system (OS) loader, set the value of this parameter to <c>false</c>. 
        /// The OS loader modifies the contents of PE image when loading it for execution (for example, when the image was 
        /// passed to the <c>LoadLibrary</c> Windows API function): it recalculates absolute addresses if the actual base address 
        /// is different from image base this file was compiled with, extends sections with padding, etc. The 
        /// <paramref name="isFileLayout"/> enables to take these modifications into account when parsing the image.
        /// </para>
        /// </remarks>
        public MemoryImage(byte[] image,string filepath,bool isFileLayout)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (image.Length==0) throw new ArgumentException("Image should not be empty array");

            if (filepath == null) filepath = String.Empty;
            this._image = image;
            this._filepath = filepath;
            this._isfile = isFileLayout;
        }
        
        /// <summary>
        /// Gets the byte array with the image data
        /// </summary>
        public byte[] Image
        {
            get { return this._image; }
        }

        /// <summary>
        /// Gets the path of the file where this image was loaded from (could be an empty string)
        /// </summary>
        public string FilePath
        {
            get { return this._filepath; }
        }

        /// <summary>
        /// Gets the value indicating that this image is a raw PE file data, rather than an image modified 
        /// by the OS loader.
        /// </summary>
        public bool IsFileLayout
        {
            get { return this._isfile; }
        }

        /// <summary>
        /// Gets the size of this image, in bytes
        /// </summary>
        public int Size
        {
            get { return this._image.Length; }
        }

        /// <summary>
        /// Gets the stream that can be used to read the data of this image
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            return new MemoryStream(this._image,false);
        }
    }
}
