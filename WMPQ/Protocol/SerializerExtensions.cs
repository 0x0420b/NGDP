using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace WMPQ.Protocol
{
    internal static class SerializerExtensions
    {
        public static T Deserialize<T>(this Stream content)
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T) serializer.Deserialize(content);
        }

        public static string Serialize<T>(this T instance, bool omitXmlDeclaration = true)
        {
            var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = true };

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
            {
                var serializer = new XmlSerializer(typeof(T));

                var xmlns = new XmlSerializerNamespaces();
                xmlns.Add(string.Empty, string.Empty);

                serializer.Serialize(xmlWriter, instance, xmlns);
                return stringWriter.ToString();
            }
        }
    }
}