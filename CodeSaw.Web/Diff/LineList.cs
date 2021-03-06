﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSaw.Web.Diff
{
    public class LineList : List<Line>
    {
        public int FileEnd => this.LastOrDefault()?.EndPosition ?? 0;

        public IEnumerable<Line> LinesBetween(int start, int end)
        {
            return this.Where(x => (x.StartPosition < end) && (x.EndPosition > start));
        }

        public Line LineInPosition(int position)
        {
            return this.SingleOrDefault(x => x.Contains(position));
        }

        public int? LineIndexInPosition(int position)
        {
            for (var i = 0; i < this.Count; i++)
            {
                if (this[i].Contains(position))
                {
                    return i;
                }
            }

            return null;
        }

        public static LineList SplitLines(string content)
        {
            if (content == "")
            {
                return new LineList();
            }

            int offset = 0;
            var result = new LineList();

            while (offset < content.Length)
            {
                var nextNewLine = content.IndexOf('\n', offset);
                int suffix = 0;
                if (nextNewLine == -1)
                {
                    suffix = 1;
                    nextNewLine = content.Length - 1;
                }

                var line = content.Substring(offset, nextNewLine - offset + suffix);

                result.Add(new Line(offset, nextNewLine + 1, line));

                offset = nextNewLine + 1;
            }

            if (content[content.Length - 1] == '\n')
            {
                result.Add(new Line(offset, offset + 1, ""));
            }

            return result;
        }
    }
}