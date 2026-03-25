using EmployeeMasterData.Models;
using EmployeeMasterData.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.Json;
using System.Linq;
namespace EmployeeMasterData.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly string filePath;
        public EmployeeService() {
            string basePath = ConfigurationManager.AppSettings["basePath"];
            string fileName = $"EmployeeData_{DateTime.Now:ddMMMM}.json";

            filePath = Path.Combine(basePath, fileName);
        }
        private List<Employee> employees = new();
        //List<Employee> employeesFromJson = JsonHelper.DeserializeFromJson<List<Employee>>(filePath) ?? new List<Employee>();
        public void AddEmployee()
        {
            employees = JsonHelper.DeserializeFromJson<List<Employee>>(filePath) ?? new List<Employee>();
            string name = ValidationHelper.GetAlphabeticString("Enter Employee Name: ");
            while (employees.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Employee with this name already exists");
                name = ValidationHelper.GetAlphabeticString("Enter Employee Name: ");
            }
            DateOnly dob = ValidationHelper.GetValidDOB("Enter DOB (yyyy-MM-dd): ");
            string gender = ValidationHelper.GetValidGender();
            string designation = ValidationHelper.GetNonEmptyString("Enter Designation: ");
            string city = ValidationHelper.GetAlphabeticString("Enter City: ");
            string state = ValidationHelper.GetAlphabeticString("Enter State: ");
            int pin = ValidationHelper.GetValidPincode();
            string phone = ValidationHelper.GetValidContact();
            while (employees.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Employee with this phone number already exists");
                phone = ValidationHelper.GetValidContact();
            }
            string email = ValidationHelper.GetValidEmail();
            while (employees.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Employee with this email already exists");
                email = ValidationHelper.GetValidEmail();
            }
            DateOnly doj = ValidationHelper.GetValidDOJ("Enter DOJ (yyyy-MM-dd): ", dob);
            string remarks = ValidationHelper.GetNonEmptyString("Enter Remarks: ");
            Department department = ValidationHelper.GetValidDepartment();
            int salary = ValidationHelper.GetValidSalary("Enter Salary: ");
            Console.WriteLine("Employee added successfully");

            Employee employee = new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                DOB = dob,
                Gender = gender[0],
                Designation = designation,
                City = city,
                State = state,
                Pincode = pin,
                PhoneNumber = phone,
                Email = email,
                DatOfJoining = doj,
                //TotExperience = ValidationHelper.CalculateExperience(doj),
                TotExperience = DateTime.Today.Year - doj.Year - (DateTime.Today.DayOfYear < doj.DayOfYear ? 1 : 0),
                Remarks = remarks,
                Department = department,
                Salary = salary
            };
            SaveEmployee(employee);
        }
        private void SaveEmployee(Employee employee)
        {
            try
            {
                //employees = JsonHelper.DeserializeFromJson<List<Employee>>(filePath) ?? new List<Employee>();
                employees.Add(employee);
                employees = employees.OrderByDescending(e => e.Salary).ToList();
                JsonHelper.SerializeToJson(filePath, employees);

                Console.WriteLine("Employee saved successfully!");
                Console.WriteLine($"Total employees: {employees.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving employee: {ex.Message}");
            }
        }

        public void DeleteEmployee()
        {
            employees = JsonHelper.DeserializeFromJson<List<Employee>>(filePath) ?? new List<Employee>();
            if (employees.Count == 0)
            {
                Console.WriteLine("No employees to delete");
                return;
            }
            Console.WriteLine("Available employees are: ");
            DisplayEmployees(employees);
            Guid id = ValidationHelper.GetValidGuid("Enter Employee ID to delete: ");
            var emp = employees.FirstOrDefault(e => e.Id == id);
            if (emp == null)
            {
                Console.WriteLine("Employee not found");
                return;
            }
            employees.Remove(emp);
            //File.WriteAllText(filePath, JsonHelper.SerializeToJson(filePath, employees));
            File.WriteAllText(filePath, JsonSerializer.Serialize(employees, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("Employee deleted successfully");
        }

        private void DisplayEmployees(List<Employee> empl)
        {
            if (empl.Count == 0)
            {
                Console.WriteLine("No employees found");
                return;
            }
            foreach (var emp in empl)
            {
                Console.WriteLine($"ID: {emp.Id}");
                Console.WriteLine($"Name: {emp.Name}");
                Console.WriteLine($"Email: {emp.Email}");
                Console.WriteLine($"Phone: {emp.PhoneNumber}");
                Console.WriteLine($"Department {emp.Department}");
                Console.WriteLine($"Designation: {emp.Designation} ");
                Console.WriteLine($"Salary: {emp.Salary} ");
            }
        }
    }
}
