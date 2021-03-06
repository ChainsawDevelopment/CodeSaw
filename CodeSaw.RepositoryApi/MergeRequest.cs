﻿using System;

namespace CodeSaw.RepositoryApi
{
    public class MergeRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public UserInfo Author { get; set; }
        public string BaseCommit { get; set; }
        public string HeadCommit { get; set; }
        public string Description { get; set; }
        public string WebUrl { get; set; }
        public MergeRequestState State { get; set; }

        public MergeStatus MergeStatus { get; set; }

        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }

        public DateTime? MergedAt {get; set; }
        public DateTime? ClosedAt {get; set; }
    }
}