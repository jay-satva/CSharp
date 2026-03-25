using System.Xml.Serialization;

namespace Bank.Utils
{
    public static class XmlHelper
    {
        public static void SerializeToXml<T>(string path, T data)
        {
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using FileStream fs = new FileStream(path, FileMode.Create);
            serializer.Serialize(fs, data);
        }
        public static T DeserializeFromXml<T>(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Account file not found.");

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using FileStream fs = new FileStream(path, FileMode.Open);
            return (T)serializer.Deserialize(fs);
        }
    }
}