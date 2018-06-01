using System;
using GitLab;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Web.Auth;
using Web.Modules.Db;

namespace Web
{
    public class CachedGitAccessTokenSource : IGitAccessTokenSource
    {
        private readonly Lazy<string> _accessTokenLazy;

        public string AccessToken => _accessTokenLazy.Value;

        public CachedGitAccessTokenSource(UserManager<ReviewUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            Console.WriteLine("New CachedGitAccessTokenSource instance");

            _accessTokenLazy = new Lazy<string>(() =>
            {
                var name = httpContextAccessor.HttpContext.User.Identity.Name;
                Console.WriteLine($"Retrieving Access Token for user {name}");

                var currentUser = userManager.FindByNameAsync(name).Result;

                return currentUser?.Token;
            });
        }
    }
}