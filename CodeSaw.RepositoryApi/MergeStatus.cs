namespace CodeSaw.RepositoryApi
{
    public enum MergeStatus
    {
        can_be_merged,
        cannot_be_merged,
        @unchecked,
        checking,
        cannot_be_merged_recheck
    }
}