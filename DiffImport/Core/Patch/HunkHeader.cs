﻿// Copyright (C) 2013, Tim Rogers <virmitio@gmail.com>
// This file has been modified for use in this project.
// The original copyright has been preserved below.

/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Text;
using GitSharpImport.Core.Diff;
using GitSharpImport.Core.Util;

namespace GitSharpImport.Core.Patch
{
    #region Old HunkHeader
    /*
	/// <summary>
	/// Hunk header describing the layout of a single block of lines.
	/// </summary>
	internal class HunkHeader
	{
		private readonly FileHeader _file;
		private readonly OldImage _oldImage;
		private readonly int _startOffset;

		internal HunkHeader(FileHeader fh, int offset)
			: this(fh, offset, new OldImage(fh))
		{
		}

		internal HunkHeader(FileHeader fh, int offset, OldImage oi)
		{
			_file = fh;
			_startOffset = offset;
			_oldImage = oi;
		}

		/// <summary>
		/// Header for the file this hunk applies to.
		/// </summary>
		internal virtual FileHeader File
		{
			get { return _file; }
		}

		/// <summary>
		/// The byte array holding this hunk's patch script.
		/// </summary>
		/// <returns></returns>
		internal byte[] Buffer
		{
			get { return _file.Buffer; }
		}

		/// <summary>
		/// Offset within <seealso cref="FileHeader.Buffer"/> to the "@@ -" line.
		/// </summary>
		internal int StartOffset
		{
			get { return _startOffset; }
		}

		/// <summary>
		/// Position 1 past the end of this hunk within <see cref="File"/>'s buffer.
		/// </summary>
		internal int EndOffset { get; set; }

		internal virtual OldImage OldImage
		{
			get { return _oldImage; }
		}

		/// <summary>
		/// First line number in the post-image file where the hunk starts.
		/// </summary>
		internal int NewStartLine { get; set; }

		/// <summary>
		/// Total number of post-image lines this hunk covers (context + inserted)
		/// </summary>
		internal int NewLineCount { get; set; }

		/// <summary>
		/// Total number of lines of context appearing in this hunk.
		/// </summary>
		internal int LinesContext { get; set; }

		/// <summary>
		/// Returns a list describing the content edits performed within the hunk.
		/// </summary>
		/// <returns></returns>
		internal EditList ToEditList()
		{
			var r = new EditList();
			byte[] buf = _file.Buffer;
			int c = RawParseUtils.nextLF(buf, _startOffset);
			int oLine = _oldImage.StartLine;
			int nLine = NewStartLine;
			Edit inEdit = null;

			for (; c < EndOffset; c = RawParseUtils.nextLF(buf, c))
			{
				bool breakScan;

				switch (buf[c])
				{
					case (byte)' ':
					case (byte)'\n':
						inEdit = null;
						oLine++;
						nLine++;
						continue;

					case (byte)'-':
						if (inEdit == null)
						{
							inEdit = new Edit(oLine - 1, nLine - 1);
							r.Add(inEdit);
						}
						oLine++;
						inEdit.ExtendA();
						continue;

					case (byte)'+':
						if (inEdit == null)
						{
							inEdit = new Edit(oLine - 1, nLine - 1);
							r.Add(inEdit);
						}
						nLine++;
						inEdit.ExtendB();
						continue;

					case (byte)'\\': // Matches "\ No newline at end of file"
						continue;

					default:
						breakScan = true;
						break;
				}

				if (breakScan)
				{
					break;
				}
			}

			return r;
		}

		internal virtual void parseHeader()
		{
			// Parse "@@ -236,9 +236,9 @@ protected boolean"
			//
			byte[] buf = _file.Buffer;
			var ptr = new MutableInteger
						{
							value = RawParseUtils.nextLF(buf, _startOffset, (byte)' ')
						};

			_oldImage.StartLine = -1 * RawParseUtils.parseBase10(buf, ptr.value, ptr);

			_oldImage.LineCount = buf[ptr.value] == ',' ? RawParseUtils.parseBase10(buf, ptr.value + 1, ptr) : 1;
			NewStartLine = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
			NewLineCount = buf[ptr.value] == ',' ? RawParseUtils.parseBase10(buf, ptr.value + 1, ptr) : 1;
		}

		internal virtual int parseBody(Patch script, int end)
		{
			byte[] buf = _file.Buffer;
			int c = RawParseUtils.nextLF(buf, _startOffset), last = c;

			_oldImage.LinesDeleted = 0;
			_oldImage.LinesAdded = 0;

			for (; c < end; last = c, c = RawParseUtils.nextLF(buf, c))
			{
				bool breakScan;
				switch (buf[c])
				{
					case (byte)' ':
					case (byte)'\n':
						LinesContext++;
						continue;

					case (byte)'-':
						_oldImage.LinesDeleted++;
						continue;

					case (byte)'+':
						_oldImage.LinesAdded++;
						continue;

					case (byte)'\\': // Matches "\ No newline at end of file"
						continue;

					default:
						breakScan = true;
						break;
				}

				if (breakScan)
				{
					break;
				}
			}

			if (last < end && LinesContext + _oldImage.LinesDeleted - 1 == _oldImage.LineCount
				&& LinesContext + _oldImage.LinesAdded == NewLineCount
				&& RawParseUtils.match(buf, last, Patch.SigFooter) >= 0)
			{
				// This is an extremely common occurrence of "corruption".
				// Users add footers with their signatures After this mark,
				// and git diff adds the git executable version number.
				// Let it slide; the hunk otherwise looked sound.
				//
				_oldImage.LinesDeleted--;
				return last;
			}

			if (LinesContext + _oldImage.LinesDeleted < _oldImage.LineCount)
			{
				int missingCount = _oldImage.LineCount - (LinesContext + _oldImage.LinesDeleted);
				script.error(buf, _startOffset, "Truncated hunk, at least "
												+ missingCount + " old lines is missing");
			}
			else if (LinesContext + _oldImage.LinesAdded < NewLineCount)
			{
				int missingCount = NewLineCount - (LinesContext + _oldImage.LinesAdded);
				script.error(buf, _startOffset, "Truncated hunk, at least "
												+ missingCount + " new lines is missing");
			}
			else if (LinesContext + _oldImage.LinesDeleted > _oldImage.LineCount
					 || LinesContext + _oldImage.LinesAdded > NewLineCount)
			{
				string oldcnt = _oldImage.LineCount + ":" + NewLineCount;
				string newcnt = (LinesContext + _oldImage.LinesDeleted) + ":"
								+ (LinesContext + _oldImage.LinesAdded);
				script.warn(buf, _startOffset, "Hunk header " + oldcnt
											   + " does not match body line count of " + newcnt);
			}

			return c;
		}

		internal void extractFileLines(TemporaryBuffer[] outStream)
		{
			byte[] buf = _file.Buffer;
			int ptr = _startOffset;
			int eol = RawParseUtils.nextLF(buf, ptr);
			if (EndOffset <= eol)
				return;

			// Treat the hunk header as though it were from the ancestor,
			// as it may have a function header appearing After it which
			// was copied out of the ancestor file.
			//
			outStream[0].write(buf, ptr, eol - ptr);

			bool break_scan = false;
			for (ptr = eol; ptr < EndOffset; ptr = eol)
			{
				eol = RawParseUtils.nextLF(buf, ptr);
				switch (buf[ptr])
				{
					case (byte)' ':
					case (byte)'\n':
					case (byte)'\\':
						outStream[0].write(buf, ptr, eol - ptr);
						outStream[1].write(buf, ptr, eol - ptr);
						break;
					case (byte)'-':
						outStream[0].write(buf, ptr, eol - ptr);
						break;
					case (byte)'+':
						outStream[1].write(buf, ptr, eol - ptr);
						break;
					default:
						break_scan = true;
						break;
				}
				if (break_scan)
					break;
			}
		}

		internal virtual void extractFileLines(StringBuilder sb, string[] text, int[] offsets)
		{
			byte[] buf = _file.Buffer;
			int ptr = _startOffset;
			int eol = RawParseUtils.nextLF(buf, ptr);
			if (EndOffset <= eol)
				return;
			copyLine(sb, text, offsets, 0);

			bool break_scan = false;
			for (ptr = eol; ptr < EndOffset; ptr = eol)
			{
				eol = RawParseUtils.nextLF(buf, ptr);
				switch (buf[ptr])
				{
					case (byte)' ':
					case (byte)'\n':
					case (byte)'\\':
						copyLine(sb, text, offsets, 0);
						skipLine(text, offsets, 1);
						break;
					case (byte)'-':
						copyLine(sb, text, offsets, 0);
						break;
					case (byte)'+':
						copyLine(sb, text, offsets, 1);
						break;
					default:
						break_scan = true;
						break;
				}
				if (break_scan)
					break;
			}
		}

		internal void copyLine(StringBuilder sb, string[] text, int[] offsets, int fileIdx)
		{
			string s = text[fileIdx];
			int start = offsets[fileIdx];
			int end = s.IndexOf('\n', start);
			if (end < 0)
			{
				end = s.Length;
			}
			else
			{
				end++;
			}
			sb.Append(s, start, end - start);
			offsets[fileIdx] = end;
		}

		internal void skipLine(string[] text, int[] offsets, int fileIdx)
		{
			string s = text[fileIdx];
			int end = s.IndexOf('\n', offsets[fileIdx]);
			offsets[fileIdx] = end < 0 ? s.Length : end + 1;
		}
	}
    */
    #endregion

    internal class HunkHeader
    {
        // private readonly FileHeader _file;
        // private readonly OldImage _oldImage;
        private readonly int _startOffset;

        /*
        internal Hunk(FileHeader fh, int offset)
            : this(fh, offset, new OldImage(fh))
        {
        }

        internal Hunk(FileHeader fh, int offset, OldImage oi)
        {
//            _file = fh;
//            _startOffset = offset;
//            _oldImage = oi;
        }
        */

        internal HunkHeader(byte[] data, int offset, byte[] oldFileBytes = null)
        {
            Buffer = data;
            _startOffset = offset;
            _oldBytes = oldFileBytes;
        }

        private byte[] _oldBytes;

        /*
        /// <summary>
        /// Header for the file this hunk applies to.
        /// </summary>
        internal virtual FileHeader File
        {
            get { return _file; }
        }
        */

        /// <summary>
        /// The byte array holding this hunk's patch script.
        /// </summary>
        /// <returns></returns>
        internal byte[] Buffer { get; private set; }

        
        /// <summary>
        /// Offset within <seealso cref="FileHeader.Buffer"/> to the "@@ -" line.
        /// </summary>
        internal int StartOffset
        {
            get { return _startOffset; }
        }

        /// <summary>
        /// Position 1 past the end of this hunk within <see cref="Buffer"/>.
        /// </summary>
        internal int EndOffset { get; set; }

        /*
        internal virtual OldImage OldImage
        {
            get { return _oldImage; }
        }
        */

        internal int OldStartLine { get; set; }
        internal int OldLineCount { get; set; }
        internal int OldLinesDeleted { get; set; }
        internal int OldLinesAdded { get; set; }


        /// <summary>
        /// First line number in the post-image file where the hunk starts.
        /// </summary>
        internal int NewStartLine { get; set; }

        /// <summary>
        /// Total number of post-image lines this hunk covers (context + inserted)
        /// </summary>
        internal int NewLineCount { get; set; }

        /// <summary>
        /// Total number of lines of context appearing in this hunk.
        /// </summary>
        internal int LinesContext { get; set; }

        /// <summary>
        /// Returns a list describing the content edits performed within the hunk.
        /// </summary>
        /// <returns></returns>
        internal EditList ToEditList()
        {
            var r = new EditList();
            byte[] buf = Buffer;
            int c = RawParseUtils.nextLF(buf, _startOffset);
            int oLine = OldStartLine;
            int nLine = NewStartLine;
            Edit inEdit = null;

            for (; c < Buffer.Length; c = RawParseUtils.nextLF(buf, c))
            {
                bool breakScan;

                switch (buf[c])
                {
                    case (byte)' ':
                    case (byte)'\n':
                        inEdit = null;
                        oLine++;
                        nLine++;
                        continue;

                    case (byte)'-':
                        if (inEdit == null)
                        {
                            inEdit = new Edit(oLine - 1, nLine - 1);
                            r.Add(inEdit);
                        }
                        oLine++;
                        inEdit.ExtendA();
                        continue;

                    case (byte)'+':
                        if (inEdit == null)
                        {
                            inEdit = new Edit(oLine - 1, nLine - 1);
                            r.Add(inEdit);
                        }
                        nLine++;
                        inEdit.ExtendB();
                        continue;

                    case (byte)'\\': // Matches "\ No newline at end of file"
                        continue;

                    default:
                        breakScan = true;
                        break;
                }

                if (breakScan)
                {
                    break;
                }
            }

            return r;
        }

        internal virtual void parseHeader()
        {
            // Parse "@@ -236,9 +236,9 @@ protected boolean"
            //
            byte[] buf = Buffer;
            var ptr = new MutableInteger
            {
                value = RawParseUtils.nextLF(buf, _startOffset, (byte)' ')
            };

            OldStartLine = -1 * RawParseUtils.parseBase10(buf, ptr.value, ptr);

            OldLineCount = buf[ptr.value] == ',' ? RawParseUtils.parseBase10(buf, ptr.value + 1, ptr) : 1;
            NewStartLine = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
            NewLineCount = buf[ptr.value] == ',' ? RawParseUtils.parseBase10(buf, ptr.value + 1, ptr) : 1;
        }

        internal virtual int parseBody(Patch script, int end)
        {
            byte[] buf = Buffer;
            int c = RawParseUtils.nextLF(buf, _startOffset), last = c;

            OldLinesDeleted = 0;
            OldLinesAdded = 0;

            for (; c < end; last = c, c = RawParseUtils.nextLF(buf, c))
            {
                bool breakScan;
                switch (buf[c])
                {
                    case (byte)' ':
                    case (byte)'\n':
                        LinesContext++;
                        continue;

                    case (byte)'-':
                        OldLinesDeleted++;
                        continue;

                    case (byte)'+':
                        OldLinesAdded++;
                        continue;

                    case (byte)'\\': // Matches "\ No newline at end of file"
                        continue;

                    default:
                        breakScan = true;
                        break;
                }

                if (breakScan)
                {
                    break;
                }
            }

            if (last < end && LinesContext + OldLinesDeleted - 1 == OldLineCount
                && LinesContext + OldLinesAdded == NewLineCount
                && RawParseUtils.match(buf, last, Patch.SigFooter) >= 0)
            {
                // This is an extremely common occurrence of "corruption".
                // Users add footers with their signatures After this mark,
                // and git diff adds the git executable version number.
                // Let it slide; the hunk otherwise looked sound.
                //
                OldLinesDeleted--;
                return last;
            }

            if (LinesContext + OldLinesDeleted < OldLineCount)
            {
                int missingCount = OldLineCount - (LinesContext + OldLinesDeleted);
                //script.error(buf, _startOffset, "Truncated hunk, at least " + missingCount + " old lines is missing");
                throw new FormatException(String.Format("Truncated hunk, at least {0} old line(s) missing.  Hunk start @ {1}.  Hunk:\n{2}",missingCount, _startOffset, buf));
            }
            else if (LinesContext + OldLinesAdded < NewLineCount)
            {
                int missingCount = NewLineCount - (LinesContext + OldLinesAdded);
                //script.error(buf, _startOffset, "Truncated hunk, at least " + missingCount + " new lines is missing");
                throw new FormatException(String.Format("Truncated hunk, at least {0} new line(s) missing.  Hunk start @ {1}.  Hunk:\n{2}", missingCount, _startOffset, buf));
            }
            else if (LinesContext + OldLinesDeleted > OldLineCount
                     || LinesContext + OldLinesAdded > NewLineCount)
            {
                string oldcnt = OldLineCount + ":" + NewLineCount;
                string newcnt = (LinesContext + OldLinesDeleted) + ":"
                                + (LinesContext + OldLinesAdded);
                //script.warn(buf, _startOffset, "Hunk header " + oldcnt + " does not match body line count of " + newcnt);
                throw new FormatException(String.Format("Hunk header {0} does not match body line count of {1}.  Hunk start @ {1}.  Hunk:\n{2}" + oldcnt, newcnt, _startOffset, buf));
            }

            return c;
        }

        internal void extractFileLines(TemporaryBuffer[] outStream)
        {
            byte[] buf = Buffer;
            int ptr = _startOffset;
            int eol = RawParseUtils.nextLF(buf, ptr);
            if (EndOffset <= eol)
                return;

            // Treat the hunk header as though it were from the ancestor,
            // as it may have a function header appearing After it which
            // was copied out of the ancestor file.
            //
            outStream[0].write(buf, ptr, eol - ptr);

            bool break_scan = false;
            for (ptr = eol; ptr < EndOffset; ptr = eol)
            {
                eol = RawParseUtils.nextLF(buf, ptr);
                switch (buf[ptr])
                {
                    case (byte)' ':
                    case (byte)'\n':
                    case (byte)'\\':
                        outStream[0].write(buf, ptr, eol - ptr);
                        outStream[1].write(buf, ptr, eol - ptr);
                        break;
                    case (byte)'-':
                        outStream[0].write(buf, ptr, eol - ptr);
                        break;
                    case (byte)'+':
                        outStream[1].write(buf, ptr, eol - ptr);
                        break;
                    default:
                        break_scan = true;
                        break;
                }
                if (break_scan)
                    break;
            }
        }

        internal byte[] ResultLines()
        {
            TemporaryBuffer output = new LocalFileBuffer();
            try
            {
                byte[] buf = Buffer;
                int ptr = _startOffset;
                int eol = RawParseUtils.nextLF(buf, ptr);
                if (EndOffset <= eol)
                    return output.ToArray();

                // Treat the hunk header as though it were from the ancestor,
                // as it may have a function header appearing After it which
                // was copied out of the ancestor file.
                //
                //            outStream[0].write(buf, ptr, eol - ptr);

                bool break_scan = false;
                for (ptr = eol; ptr < EndOffset; ptr = eol)
                {
                    eol = RawParseUtils.nextLF(buf, ptr);
                    switch (buf[ptr])
                    {
                        case (byte) ' ':
                        case (byte) '\n':
                        case (byte) '\\':
                            output.write(buf, ptr, eol - ptr);
                            //output.write(buf, ptr + 1, eol - (ptr + 1));
                            break;
                        case (byte) '-':
                            output.write(buf, ptr, eol - ptr);
                            break;
                        case (byte) '+':
                            output.write(buf, ptr, eol);
                            break;
                        default:
                            break_scan = true;
                            break;
                    }
                    if (break_scan)
                        break;
                }
                return output.ToArray();
            }
            finally
            {
                output.close();
            }
        }

        internal virtual void extractFileLines(StringBuilder sb, string[] text, int[] offsets)
        {
            byte[] buf = Buffer;
            int ptr = _startOffset;
            int eol = RawParseUtils.nextLF(buf, ptr);
            if (EndOffset <= eol)
                return;
            copyLine(sb, text, offsets, 0);

            bool break_scan = false;
            for (ptr = eol; ptr < EndOffset; ptr = eol)
            {
                eol = RawParseUtils.nextLF(buf, ptr);
                switch (buf[ptr])
                {
                    case (byte)' ':
                    case (byte)'\n':
                    case (byte)'\\':
                        copyLine(sb, text, offsets, 0);
                        skipLine(text, offsets, 1);
                        break;
                    case (byte)'-':
                        copyLine(sb, text, offsets, 0);
                        break;
                    case (byte)'+':
                        copyLine(sb, text, offsets, 1);
                        break;
                    default:
                        break_scan = true;
                        break;
                }
                if (break_scan)
                    break;
            }
        }

        internal void copyLine(StringBuilder sb, string[] text, int[] offsets, int fileIdx)
        {
            string s = text[fileIdx];
            int start = offsets[fileIdx];
            int end = s.IndexOf('\n', start);
            if (end < 0)
            {
                end = s.Length;
            }
            else
            {
                end++;
            }
            sb.Append(s, start, end - start);
            offsets[fileIdx] = end;
        }

        internal void skipLine(string[] text, int[] offsets, int fileIdx)
        {
            string s = text[fileIdx];
            int end = s.IndexOf('\n', offsets[fileIdx]);
            offsets[fileIdx] = end < 0 ? s.Length : end + 1;
        }
    }


	#region Nested Types
    /*
	/// <summary>
	/// Details about an old image of the file.
	/// </summary>
	internal class OldImage
	{
		private readonly FileHeader _fh;
		private readonly AbbreviatedObjectId _id;

		internal OldImage(FileHeader fh)
			: this(fh, fh.getOldId())
		{
		}

		internal OldImage(FileHeader fh, AbbreviatedObjectId id)
		{
			_fh = fh;
			_id = id;
		}

		/// <summary>
		/// Returns the <see cref="AbbreviatedObjectId"/> of the pre-image file.
		/// </summary>
		/// <returns></returns>
		internal virtual AbbreviatedObjectId Id
		{
			get { return _id; }
		}

		/// <summary>
		/// Returns the <see cref="FileHeader"/> of this hunk.
		/// </summary>
		internal virtual FileHeader Fh
		{
			get { return _fh; }
		}

		/// <summary>
		/// Return the first line number the hunk starts on in this file.
		/// </summary>
		/// <returns></returns>
		internal int StartLine { get; set; }

		/// <summary>
		/// rReturn the total number of lines this hunk covers in this file.
		/// </summary>
		internal int LineCount { get; set; }

		/// <summary>
		/// Returns the number of lines deleted by the post-image from this file.
		/// </summary>
		internal int LinesDeleted { get; set; }

		/// <summary>
		/// Returns the number of lines added by the post-image not in this file.
		/// </summary>
		internal int LinesAdded { get; set; }

		internal virtual int LinesContext
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}

	internal class CombinedOldImage : OldImage
	{
		private readonly int _imagePos;

		internal CombinedOldImage(FileHeader fh, int imagePos)
			: base(fh, ((CombinedFileHeader)fh).getOldId(imagePos))
		{
			_imagePos = imagePos;
		}

		internal override int LinesContext
		{
			get;
			set;
		}

		internal int ImagePos
		{
			get { return _imagePos; }
		}
	}
    */
	#endregion
}