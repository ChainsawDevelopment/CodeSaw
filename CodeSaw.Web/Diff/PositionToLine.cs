using System.Collections.Generic;
using System.Linq;

namespace CodeSaw.Web.Diff
{
    public class PositionToLine
    {
        private readonly SortedDictionary<int, int> _map;
        public int TotalLines { get; }

        public PositionToLine(string text)
        {
            _map = new SortedDictionary<int, int>();
            _map[0] = 0;

            int lastPosition = 0;
            int line =1;

            while (true)
            {
                var nextNewLine = text.IndexOf('\n', lastPosition);

                if (nextNewLine == -1)
                {
                    break;
                }

                _map[nextNewLine + 1] = line;
                line++;

                lastPosition = nextNewLine + 1;
            }

            _map[text.Length] = line;
            TotalLines = line;
        }

        public int GetLineinPosition(int position)
        {
            var key = _map.Keys.TakeWhile(lineStart => lineStart <= position).Last();

            return _map[key];
        }

        public int GetLineStartPosition(int line)
        {
            return _map.First(x => x.Value == line - 1).Key;
        }
    }
}