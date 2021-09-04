/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Reflection
{
    public class MemoryImage
    {
        ulong _imageBase;
        ulong _address;
        ulong _size;
        ulong _metadataAddress;
        ulong _metadataSize;
        byte[] _image;
        byte[] _metadata;

        public MemoryImage(
            ulong imageBase,
            ulong address,
            ulong size,
            ulong metadataAddress,
            ulong metadataSize,
            byte[] image,
            byte[] metadata
            )
        {
            this._imageBase = imageBase;
            this._address = address;
            this._size = size;
            this._metadataAddress = metadataAddress;
            this._metadataSize = metadataSize;
            this._image = image;
            this._metadata = metadata;
        }

        public ulong ImageBase
        {
            get { return this._imageBase; }
        }

        public ulong Address
        {
            get { return this._address; }
        }

        public ulong Size
        {
            get { return this._size; }
        }

        public ulong MetadataAddress
        {
            get { return this._metadataAddress; }
        }

        public ulong MetadataSize
        {
            get { return this._metadataSize; }
        }

        public byte[] Image
        {
            get { return this._image; }
        }

        public byte[] Metadata
        {
            get { return this._metadata; }
        }
    }
}
