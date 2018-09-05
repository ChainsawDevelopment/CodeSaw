using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSaw.Web
{
    public class MultiPartStringTokenizer<TPart>
    {
        private readonly Func<TPart, string> _extractString;

        public MultiPartStringTokenizer(Func<TPart, string> extractString)
        {
            _extractString = extractString;
        }

        public IEnumerable<Item> Enumerate(char separator, IEnumerable<TPart> parts)
        {
            var remaining = "";
            var contributingContainers = new List<TPart>();

            foreach (var partContainer in parts)
            {
                contributingContainers.Add(partContainer);
                var part = _extractString(partContainer);

                int position = 0;
                while (true)
                {
                    var nextSeparator = part.IndexOf(separator, position);

                    if (nextSeparator != -1)
                    {
                        var text = remaining + part.Substring(position, nextSeparator - position);
                        yield return new Item(text, contributingContainers.ToList());
                        position = nextSeparator + 1;
                        remaining = "";
                        contributingContainers = new List<TPart>() {contributingContainers.Last()};
                    }
                    else
                    {
                        remaining += part.Substring(position);
                        break;
                    }
                }
            }

            if (remaining != "")
            {
                yield return new Item(remaining, contributingContainers);
            }
        }

        public class Item
        {
            public string Text { get; }
            public List<TPart> Parts { get; }

            public Item(string text, List<TPart> parts)
            {
                Text = text;
                Parts = parts;
            }
        }
    }
}