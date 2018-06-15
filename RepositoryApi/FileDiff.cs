﻿using System;
using System.Collections.Generic;

namespace RepositoryApi
{
    public sealed class PathPair : IEquatable<PathPair>
    {
        public string OldPath { get; set; }
        public string NewPath { get; set; }


        public bool Equals(PathPair other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(OldPath, other.OldPath) && string.Equals(NewPath, other.NewPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(PathPair)) return false;
            return Equals((PathPair) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((OldPath != null ? OldPath.GetHashCode() : 0) * 397) ^ (NewPath != null ? NewPath.GetHashCode() : 0);
            }
        }

        public static bool operator ==(PathPair left, PathPair right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PathPair left, PathPair right)
        {
            return !Equals(left, right);
        }
    }

    public class FileDiff
    {
        public PathPair Path { get; set; } = new PathPair();
        public bool NewFile { get; set; }
        public bool RenamedFile { get; set; }
        public bool DeletedFile { get; set; }
    }
}