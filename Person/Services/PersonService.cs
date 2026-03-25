using Person.Models;
using Person.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.Json;
using System.Xml.Serialization;

namespace Person.Services
{
    public class PersonService
    {
        //List<PersonClass> persons = new();
        private readonly string filePath = "Data/persons.json";
        private readonly string xmlPath = Path.Combine("Data", "persons.xml");
        public void AddPerson(List<City> c)
        {
            if (c.Count == 0)
            {
                Console.WriteLine("No city available, add city first");
                return;
            }
            {
                string name = InputValidator.GetNonEmptyString("Enter person name");
                int age = InputValidator.GetPositiveInt("Enter person age");
                Console.WriteLine("\nAvailable cities:");
                for (int i = 0; i < c.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {c[i]}");
                }

                int cityIndex = InputValidator.GetPositiveInt("Select city number: ") - 1;

                if (cityIndex < 0 || cityIndex >= c.Count)
                {
                    Console.WriteLine("Invalid city selection.");
                    //return;
                    AddPerson(c);
                }
                PersonClass person = new()
                {
                    Name = name,
                    Age = age,
                    City = c[cityIndex]
                };
                SavePerson(person);
                Console.WriteLine("Person data has been saved");
            }
        }
        public void ShowPersons()
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("No persons found");
                return;
            }

            string json = File.ReadAllText(filePath);

            var persons = JsonSerializer.Deserialize<List<PersonClass>>(json);

            if (persons == null || persons.Count == 0)
            {
                Console.WriteLine("No persons found");
                return;
            }

            foreach (var p in persons)
            {
                Console.WriteLine(p);
            }
        }
        private void SavePerson(PersonClass person)
        {
            List<PersonClass> persons = new();
            if (File.Exists(filePath))
            {
                string existing = File.ReadAllText(filePath);
                persons = JsonSerializer.Deserialize<List<PersonClass>>(existing) ?? new();
            }

            persons.Add(person);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(persons, options);
            Directory.CreateDirectory("Data");
            File.WriteAllText(filePath, json);
            SavePersonXml(persons);
        }
        private void SavePersonXml(List<PersonClass> persons)
        {
            Directory.CreateDirectory("Data");
            XmlSerializer serializer = new XmlSerializer(typeof(List<PersonClass>));
            using FileStream fs = new FileStream(xmlPath, FileMode.Create);
            serializer.Serialize(fs, persons);
        }
    }
}
