using Nancy.Routing.Constraints;

namespace Web
{
    public class RevisionIdSegmentConstraint : RouteSegmentConstraintBase<RevisionId>
    {
        protected override bool TryMatch(string constraint, string segment, out RevisionId matchedValue)
        {
            return RevisionId.TryParse(segment, out matchedValue);
        }

        public override string Name { get; } = "revId";
    }
}