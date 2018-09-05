using System;
using CodeSaw.RepositoryApi;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class FileDiscussion : Discussion
    {
        public virtual PathPair File { get; set; }
        public virtual int LineNumber { get; set; }
    }
}