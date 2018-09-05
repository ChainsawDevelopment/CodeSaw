using System;

namespace CodeSaw.RepositoryApi
{
    [Flags]
    public enum HookEvents
    {
        Push = 1,
        MergeRequest = 2
    }
}