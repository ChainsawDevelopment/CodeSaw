using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CodeSaw.Web
{
    [TypeConverter(typeof(StringToRevisionIdConverter))]
    public abstract class RevisionId
    {
        public class Base : RevisionId, IEquatable<Base>
        {
            public override string ToString() => "Base";
            
            public override TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash) => resolveBase();
            public bool Equals(Base other) => true;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Base) obj);
            }

            public override int GetHashCode() => -1;

            public static bool operator ==(Base left, Base right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Base left, Base right)
            {
                return !Equals(left, right);
            }
        }

        public class Selected : RevisionId, IEquatable<Selected>
        {
            public int Revision { get; }

            public Selected(int revision)
            {
                Revision = revision;
            }

            public override string ToString() => $"Selected({Revision})";

            public override TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash) => resolveSelected(this);

            public bool Equals(Selected other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Revision == other.Revision;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Selected) obj);
            }

            public override int GetHashCode() => Revision;

            public static bool operator ==(Selected left, Selected right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Selected left, Selected right)
            {
                return !Equals(left, right);
            }
        }

        public class Hash : RevisionId, IEquatable<Hash>
        {
            public string CommitHash { get; }

            internal Hash(string commitHash)
            {
                CommitHash = commitHash;
            }

            public override string ToString() => $"Hash({CommitHash})";
            
            public override TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash) => resolveHash(this);

            public bool Equals(Hash other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(CommitHash, other.CommitHash);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Hash) obj);
            }

            public override int GetHashCode() => CommitHash.GetHashCode();

            public static bool operator ==(Hash left, Hash right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Hash left, Hash right)
            {
                return !Equals(left, right);
            }
        }

        private static readonly  Regex HashRegex = new Regex("^[a-fA-F0-9]{40}$");

        private RevisionId()
        {
            
        }

        public static RevisionId Parse(string s)
        {
            if (TryParse(s, out var r))
            {
                return r;
            }

            throw new ArgumentOutOfRangeException($"Value '{s}' is not recognized as valid revision ID");
        }

        public abstract TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash);

        public static bool TryParse(string s, out RevisionId revisionId)
        {
            if (s == "base")
            {
                revisionId= new Base();
                return true;
            }

            if (int.TryParse(s, out var revision) && revision > 0)
            {
                revisionId= new Selected(revision);
                return true;
            }

            if (HashRegex.IsMatch(s))
            {
                revisionId= new Hash(s);
                return true;
            }

            revisionId = null;

            return false;
        }
    }

    public class StringToRevisionIdConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => RevisionId.Parse(value.ToString());
    }
}