using System;
using System.Text.RegularExpressions;

namespace Web
{
    public abstract class RevisionId
    {
        public class Base : RevisionId
        {
            public override string ToString() => "Base";
            
            public override TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash) => resolveBase();
        }

        public class Selected : RevisionId
        {
            public int Revision { get; }

            internal Selected(int revision)
            {
                Revision = revision;
            }

            public override string ToString() => $"Selected({Revision})";

            public override TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash) => resolveSelected(this);
        }

        public class Hash : RevisionId
        {
            public string CommitHash { get; }

            internal Hash(string commitHash)
            {
                CommitHash = commitHash;
            }

            public override string ToString() => $"Hash({CommitHash})";
            
            public override TResult Resolve<TResult>(Func<TResult> resolveBase, Func<Selected, TResult> resolveSelected, Func<Hash, TResult> resolveHash) => resolveHash(this);
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
}