using System.Reflection;

namespace CodeSaw.Db.Migrations
{
    public static class Marker
    {
        public static readonly Assembly ThisAssembly = typeof(Marker).Assembly;
    }
}
