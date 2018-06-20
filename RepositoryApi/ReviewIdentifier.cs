using System;

namespace RepositoryApi
{
    public class ReviewIdentifier : IEquatable<ReviewIdentifier>
    {
        public int ProjectId { get; protected set; }
        public int ReviewId { get; protected set; }

        private ReviewIdentifier()
        {
            
        }

        public ReviewIdentifier(int projectId, int reviewId)
        {
            ProjectId = projectId;
            ReviewId = reviewId;
        }

        public bool Equals(ReviewIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProjectId == other.ProjectId && ReviewId == other.ReviewId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ReviewIdentifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ProjectId * 397) ^ ReviewId;
            }
        }

        public static bool operator ==(ReviewIdentifier left, ReviewIdentifier right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ReviewIdentifier left, ReviewIdentifier right)
        {
            return !Equals(left, right);
        }
    }
}