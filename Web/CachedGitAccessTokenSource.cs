using System;
using GitLab;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Web.Auth;
using Web.Modules.Api.Commands;
using Web.Modules.Db;

namespace Web
{
    public class CachedGitAccessTokenSource : IGitAccessTokenSource
    {
        private readonly Lazy<string> _accessTokenLazy;

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