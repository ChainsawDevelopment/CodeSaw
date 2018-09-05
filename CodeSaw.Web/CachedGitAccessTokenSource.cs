using System;
using CodeSaw.GitLab;
using CodeSaw.Web.Auth;
using Microsoft.AspNetCore.Identity;

namespace CodeSaw.Web
{
    public class CachedGitAccessTokenSource : IGitAccessTokenSource
    {
        private readonly Lazy<string> _accessTokenLazy;

        public TokenType Type { get; } = TokenType.OAuth;

        public string AccessToken => _accessTokenLazy.Value;

        public CachedGitAccessTokenSource(UserManager<ReviewUser> userManager, [CurrentUser]ReviewUser currentUser)
        {
            Console.WriteLine("New CachedGitAccessTokenSource instance");

            _accessTokenLazy = new Lazy<string>(() =>
            {
                Console.WriteLine($"Retrieving Access Token for user {currentUser.UserName}");

                return currentUser?.Token;
            });
        }
    }
}