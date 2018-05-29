using System.Reflection;

namespace Db.Migrations
{
    public static class Marker
    {
        public static readonly Assembly ThisAssembly = typeof(Marker).Assembly;
    }
}
