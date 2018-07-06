using System;

namespace RepositoryApi
{
    [Flags]
    public enum HookEvents
    {
        Push = 1,
        MergeRequest = 2
    }
}