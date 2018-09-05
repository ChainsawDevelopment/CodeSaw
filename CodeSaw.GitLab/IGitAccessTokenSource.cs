namespace CodeSaw.GitLab
{
    public interface IGitAccessTokenSource
    {
        TokenType Type { get; }
        string AccessToken { get; }
    }

    public enum TokenType
    {
        OAuth,
        Custom
    }
}