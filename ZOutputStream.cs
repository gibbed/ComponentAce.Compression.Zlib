// Copyright (c) 2006, ComponentAce
// http://www.componentace.com
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
// Neither the name of ComponentAce nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission. 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

/*
Copyright (c) 2001 Lapo Luchini.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in 
the documentation and/or other materials provided with the distribution.

3. The names of the authors may not be used to endorse or promote products
derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS
OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

/*
* This program is based on zlib-1.1.3, so all credit should go authors
* Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
* and contributors of zlib.
*/

using System;
using System.IO;

namespace ComponentAce.Compression.Zlib
{
	public class ZOutputStream : Stream
	{
        private void InitBlock()
        {
            this._FlushMode = zlibConst.Z_NO_FLUSH;
            this.Buffer = new byte[this.BufferSize];
        }

        virtual public int FlushMode
        {
            get
            {
                return (_FlushMode);
            }

            set
            {
                this._FlushMode = value;
            }
        }

        /// <summary>
        /// Returns the total number of bytes input so far.
        /// </summary>
		virtual public long TotalIn
		{
			get { return z.total_in; }
		}

		/// <summary>
        /// Returns the total number of bytes output so far.
		/// </summary>
		virtual public long TotalOut
		{
			get { return z.total_out; }
        }
		
		protected internal ZStream z = new ZStream();
		protected internal int BufferSize = 4096;		
		protected internal int _FlushMode;		
		protected internal byte[] Buffer, OneByte = new byte[1];
		protected internal bool Compressing;
		
		private Stream Output;

        public ZOutputStream(Stream output)
            : base()
        {
            this.InitBlock();
            this.Output = output;
            z.inflateInit();
            this.Compressing = false;
        }

        public ZOutputStream(Stream output, int level)
            : base()
        {
            this.InitBlock();
            this.Output = output;
            z.deflateInit(level);
            this.Compressing = true;
        }

        public override void WriteByte(byte value)
        {
            this.OneByte[0] = value;
            this.Write(this.OneByte, 0, 1);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
            {
                return;
            }

            int err;

            var b = new byte[buffer.Length];
                Array.Copy(buffer, 0, b, 0, buffer.Length);

            z.next_in = b;
            z.next_in_index = offset;
            z.avail_in = count;

            do
            {
                z.next_out = this.Buffer;
                z.next_out_index = 0;
                z.avail_out = BufferSize;

                if (this.Compressing == true)
                {
                    err = z.deflate(this._FlushMode);
                }
                else
                {
                    err = z.inflate(this._FlushMode);
                }

                if (err != zlibConst.Z_OK && err != zlibConst.Z_STREAM_END)
                {
                    throw new ZStreamException((this.Compressing ? "de" : "in") + "flating: " + z.msg);
                }

                this.Output.Write(this.Buffer, 0, this.BufferSize - z.avail_out);
            }
            while (z.avail_in > 0 || z.avail_out == 0);
        }

        public virtual void Finish()
        {
            int err;

            do
            {
                z.next_out = this.Buffer;
                z.next_out_index = 0;
                z.avail_out = this.BufferSize;

                if (this.Compressing == true)
                {
                    err = z.deflate(zlibConst.Z_FINISH);
                }
                else
                {
                    err = z.inflate(zlibConst.Z_FINISH);
                }

                if (err != zlibConst.Z_STREAM_END && err != zlibConst.Z_OK)
                {
                    throw new ZStreamException((Compressing ? "de" : "in") + "flating: " + z.msg);
                }

                if (this.BufferSize - z.avail_out > 0)
                {
                    this.Output.Write(Buffer, 0, BufferSize - z.avail_out);
                }
            }
            while (z.avail_in > 0 || z.avail_out == 0);

            if (this.Compressing == true)
            {
                z.deflateEnd();
            }
            else
            {
                z.inflateEnd();
            }

            z.free();
            z = null;

            this.Flush();
        }

        public override void Close()
        {
            this.Finish();
            this.Output.Close();
            this.Output = null;
        }

        public override void Flush()
        {
            this.Output.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
	}
}