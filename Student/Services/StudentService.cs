using Student.Models;
using Student.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Student.Services
{
    public class StudentService
    {
        List<StudentClass> students = new();
        public readonly string filePath = "Data/students.json";

        public void AddStudent()
        {
            //if (File.Exists(filePath) && !string.IsNullOrEmpty(File.ReadAllText(filePath)))
            //{
            //    int Id = students.Count > 0 ? students.Max(e => e.Id) + 1 : 1;
            //}
            students = File.Exists(filePath) && !string.IsNullOrEmpty(File.ReadAllText(filePath))
                     ? JsonSerializer.Deserialize<List<StudentClass>>(File.ReadAllText(filePath))!
                     : new List<StudentClass>();
            int id = students.Count > 0 ? students.Max(e => e.Id) + 1 : 1;
            string firstName = StringValidator.GetNonEmptyString("Enter First Name: ");
            string lastName = StringValidator.GetNonEmptyString("Enter Last Name: ");
            string department = StringValidator.GetNonEmptyString("Enter Department Name: ");
            string rollNo = StringValidator.GetValidRollNo();
            int semester = IntValidator.GetValidSem();
            string dob = StringValidator.GetNonEmptyString("Enter DOB: ");
            int age = IntValidator.GetValidAge();
            string contact = StringValidator.GetValidContact();
            string address = StringValidator.GetNonEmptyString("Enter Address: ");

            StudentClass student = new()
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                Department = department,
                RollNo = rollNo,
                Semester = semester,
                DOB = dob,
                Age = age,
                Contact = contact,
                Address = address
            };
            SaveStudent(student);
        }

        private void SaveStudent(StudentClass student)
        {
            if (File.Exists(filePath))
            {
                string existing = File.ReadAllText(filePath);
                students = JsonSerializer.Deserialize<List<StudentClass>>(existing) ?? new();
            }
            students.Add(student);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(students, options);
            Directory.CreateDirectory("Data");
            File.WriteAllText(filePath, json);
            Console.WriteLine("Student admission is done!");
            Console.WriteLine("Total number of students are "+ students.Count);
        }
        public void ShowStudent()
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("No Students found");
                return;
            }

            string json = File.ReadAllText(filePath);
            var persons = JsonSerializer.Deserialize<List<StudentClass>>(json);
            if (persons == null || persons.Count == 0)
            {
                Console.WriteLine("No Students found");
                return;
            }
            foreach (var p in persons)
            {
                Console.WriteLine($"{p.FirstName} {p.LastName}, Department: {p.Department}, RollNo: {p.RollNo}, Semester: {p.Semester}, DOB: {p.DOB}, Age: {p.Age}, Contact: {p.Contact}, Address: {p.Address}");
            }
        }
        public void RemoveStudent()
        {
            Console.WriteLine("Enter id to remove student");
            int id;
            while (!int.TryParse(Console.ReadLine(), out id))
                Console.WriteLine("Enter a valid Id");
            var stdnt = students.FirstOrDefault(e => e.Id == id);
            if (stdnt == null)
            {
                Console.WriteLine("Student not found");
                return;
            }

            students.Remove(stdnt);
            //SaveStudent(student);
            File.WriteAllText(filePath, JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = true }));

            Console.WriteLine("Student deleted successfully");
        }
        public void ShowStudentByID()
        {
            Console.WriteLine("Enter Student ID");
            int id;
            while (!int.TryParse(Console.ReadLine(), out id))
                Console.WriteLine("Enter a valid Id");
            var stdnt = students.FirstOrDefault(e => e.Id == id);
            if (stdnt == null)
            {
                Console.WriteLine("Student not found");
                return;
            }
            Console.WriteLine("First Name: "+ stdnt.FirstName);
            Console.WriteLine("Last Name: "+ stdnt.LastName);
            Console.WriteLine("Department Name: "+ stdnt.Department);
            Console.WriteLine("Roll No: "+ stdnt.RollNo);
            Console.WriteLine("Semester: "+ stdnt.Semester);
            Console.WriteLine("DOB: "+ stdnt.DOB);
            Console.WriteLine("Age: "+ stdnt.Age);
            Console.WriteLine("Contact: "+ stdnt.Contact);
            Console.WriteLine("Address: "+ stdnt.Address);
        }
    }
}
