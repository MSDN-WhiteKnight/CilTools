/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Reflection
{
    public class MemoryImage
    {
        byte[] _image;
        string _filepath;

        public MemoryImage(byte[] image,string filepath)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (image.Length==0) throw new ArgumentException("Image should not be empty array");

            if (filepath == null) filepath = String.Empty;
            this._image = image;
            this._filepath = filepath;
        }
        
        public byte[] Image
        {
            get { return this._image; }
        }

        public string FilePath
        {
            get { return this._filepath; }
        }

        public int Size
        {
            get { return this._image.Length; }
        }

        public Stream GetStream()
        {
            return new MemoryStream(this._image);
        }
    }
}
