using DiffMatchPatch;

namespace CodeSaw.Web.Diff
{
    public class Line
    {
        public int StartPosition { get; }
        public int EndPosition { get; }
        public string Text { get; }

        public DiffClassification Classification { get; private set; }
        public Patch Patch { get; private set; }
        public DiffMatchPatch.Diff Diff { get; private set; }
        
        public bool IsNoChange => Diff == null || Diff.Operation.IsEqual;

        public Line(int startPosition, int endPosition, string text)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Text = text;
        }

        public void AssignDiff(DiffClassification classification, Patch patch, DiffMatchPatch.Diff diff)
        {
            Classification = classification;
            Patch = patch;
            Diff = diff;
        }

        public override string ToString()
        {
            char op = ' ';
            if (Diff != null)
            {
                if (Diff.Operation.IsDelete)
                {
                    op = 'D';
                }
                else if (Diff.Operation.IsInsert)
                {
                    op = 'I';
                }
                else if (Diff.Operation.IsEqual)
                {
                    op = 'E';
                }
                
            }
            return $"[{StartPosition,4} - {EndPosition,4} {op}] {Text}";
        }

        public bool Contains(int position) => StartPosition <= position && position < EndPosition;

        public void ClearDiff()
        {
            Classification = DiffClassification.Unchanged;
            Patch = null;
            Diff = null;
        }
    }
}