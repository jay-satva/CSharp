using Person.Models;
using Person.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Text.Json;
namespace Person.Services

{
    public class CityService
    {
        private readonly string filePath = Path.Combine("Data", "cities.json");
        private readonly string xmlPath = Path.Combine("Data", "cities.xml");

        private List<City> cities = new();
        public CityService() {
            LoadCity();
        }
        public void AddCity()
        {
            string name = InputValidator.GetNonEmptyString("Add City");
            while (cities.Any(c => c.Name == name)) {
                Console.WriteLine("City already exists");
                //name = Console.ReadLine();
                name = InputValidator.GetNonEmptyString("Add City");
            }
            int population = InputValidator.GetPositiveInt("Add Population");

            City city = new()
            {
                Name = name,
                Population = population,
            };
            cities.Add(city);
            SaveCities();
            Console.WriteLine("City added successfully");
        }
        public List<City> GetCities()
        {
            return cities;
        }
        private void SaveCities()
        {
            Directory.CreateDirectory("Data");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(cities, options);
            File.WriteAllText(filePath, json);
            SaveXmlCity(cities);
        }
        public void ShowCity()
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("No cities found");
                return;
            }
            string json = File.ReadAllText(filePath);

            var cities = JsonSerializer.Deserialize<List<City>>(json);

            if (cities == null || cities.Count == 0)
            {
                Console.WriteLine("No city found");
                return;
            }
            foreach (var city in cities) { 
                Console.WriteLine(city);
            }
        }
        private void LoadCity()
        {
            if (!File.Exists(filePath))
                return;
            string json = File.ReadAllText(filePath);
            cities = JsonSerializer.Deserialize<List<City>>(json) ?? new();
        }
        private void SaveXmlCity(List<City> city)
        {
            Directory.CreateDirectory("Data");
            XmlSerializer serializer = new XmlSerializer(typeof(List<City>));
            using FileStream fs = new FileStream(xmlPath, FileMode.Create);
            serializer.Serialize(fs, city);
        }
    }
}
