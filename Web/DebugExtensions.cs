using System.IO;
using System.Xml.Serialization;
using NHibernate.Cfg.MappingSchema;

namespace Web
{
    public static class DebugExtensions
    {
        public static void DumpHbmXml(this HbmMapping mapping, string path)
        {
            var xmlSerializer = new XmlSerializer(mapping.GetType());

            using (var fileStream = File.Create(path))
            {
                xmlSerializer.Serialize(fileStream, mapping);
            }
        }
    }
}