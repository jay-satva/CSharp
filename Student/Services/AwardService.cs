using System;
using System.Collections.Generic;
using System.Text;
using Student.Models;
using Student.Utils;
using System.Text.Json;
namespace Student.Services
{
    public class AwardService
    {
        List<Awards> awards = new();
        public readonly string filePath = "~/Data/students.json";
        public void AddAward()
        {
            awards = File.Exists(filePath) && !string.IsNullOrEmpty(File.ReadAllText(filePath))
                     ? JsonSerializer.Deserialize<List<Awards>>(File.ReadAllText(filePath))!
                     : new List<Awards>();
            int id = awards.Count > 0 ? awards.Max(e => e.Id) + 1 : 1;
            string category = StringValidator.GetNonEmptyString("Enter Category");
            int rank = IntValidator.GetPositiveInt("Enter Rank");
            string studentFirstName = StringValidator.GetNonEmptyString("Enter Student First Name");
            string studentLastName = StringValidator.GetNonEmptyString("Enter Student Last Name");
            Awards award = new()
            {
                Id = id,
                Category = category,
                Rank = rank,
                StudentFirstName = studentFirstName,
                StudentLastName = studentLastName
            };
            SaveAward(award);
        }
        public void SaveAward(Awards award)
        {
            if (File.Exists(filePath))
            {
                string existing = File.ReadAllText(filePath);
                awards = JsonSerializer.Deserialize<List<Awards>>(existing) ?? new();
            }
            awards.Add(award);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(awards, options);
            File.WriteAllText(filePath, json);
        }
        public void ShowAwards()
        {
            if (File.Exists(filePath) && !string.IsNullOrEmpty(File.ReadAllText(filePath)))
            {
                awards = JsonSerializer.Deserialize<List<Awards>>(File.ReadAllText(filePath))!;
                foreach (var award in awards)
                {
                    Console.WriteLine($"Id: {award.Id}, Category: {award.Category}, Rank: {award.Rank}, Student Name: {award.StudentFirstName} {award.StudentLastName}");
                }
            }
            else
            {
                Console.WriteLine("No awards found.");
            }
        }
    }
}
