using Web.Auth;

namespace Web
{
    public interface ICurrentUser
    {
        ReviewUser CurrentUser { get; }
    }
}