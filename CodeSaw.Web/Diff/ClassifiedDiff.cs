namespace CodeSaw.Web.Diff
{
    public class ClassifiedDiff
    {
        public DiffMatchPatch.Diff Diff { get; }
        public DiffClassification Classification { get; }

        public ClassifiedDiff(DiffMatchPatch.Diff diff, DiffClassification classification)
        {
            Diff = diff;
            Classification = classification;
        }

        public override string ToString() => $"{Classification}({Diff})";
    }
}