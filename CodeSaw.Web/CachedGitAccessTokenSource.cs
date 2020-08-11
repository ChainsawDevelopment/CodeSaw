using System;
using System.Diagnostics;
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

                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    return currentUser?.Token;
                }
                finally
                {
                    sw.Stop();
                    Console.WriteLine($"Getting token for {currentUser.UserName}: {sw.ElapsedMilliseconds}ms");
                }
            });
        }
    }
}