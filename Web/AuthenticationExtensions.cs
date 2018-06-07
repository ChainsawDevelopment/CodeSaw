using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;

namespace Web
{
    public static class AuthenticationExtensions
    {
        public static void ChallengeAllUnauthenticatedCalls(this IApplicationBuilder app)
        {
            app.Use(async (context, func) =>
            {
                var userResult = await context.AuthenticateAsync();
                var user = userResult.Principal;

                var notAuthenticated = user == null || !user.Identities.Any(identity => identity.IsAuthenticated);
                if (notAuthenticated)
                {
                    await context.ChallengeAsync();
                }
                else
                {
                    await func();
                }
            });
        }
    }
}