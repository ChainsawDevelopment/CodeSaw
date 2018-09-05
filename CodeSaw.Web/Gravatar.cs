using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CodeSaw.Web
{
    public static class Gravatar
    {
        public static string HashEmail(string email)
        {
            var md5Hasher = MD5.Create();
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(email.ToLowerInvariant()));

            return string.Join("", data.Select(x => x.ToString("x2")));
        }
    }
}