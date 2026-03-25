using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SecureEMS.Utils
{
    public static class JsonHelper
    {
        public static void SerializeToJson<T>(string filePath, T data)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving JSON: {ex.Message}");
            }
        }
        public static T? DeserializeFromJson<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return default;

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading JSON: {ex.Message}");
                return default;
            }
        }
    }
}