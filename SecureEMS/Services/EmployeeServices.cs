using SecureEMS.Models;
using System;
using System.Collections.Generic;
using System.Text;
using SecureEMS.Utils;
namespace SecureEMS.Services
{
    public class EmployeeServices
    {
        //EncryptionService encryptionservice = new();
        private readonly IEncryptionService encryptionService;
        //here encryptionService is variable of type IEncryptionService interface
        private List<Employee> employees = new();
        private readonly string filePath = Path.Combine("Data", "emplloyees.json");

        public EmployeeServices(IEncryptionService encryptionService)
        {
            this.encryptionService = encryptionService;
            //now the employee can use encryption
        }

        public void AddEmployee()
        {
            string fname = ValidationHelper.GetNonEmptyString("Enter First Name: ");
            string lname = ValidationHelper.GetNonEmptyString("Enter Last Name: ");
            string email = ValidationHelper.GetValidEmail();
            string phone = ValidationHelper.GetValidContact();
            int salary = ValidationHelper.GetValidSalary("Enter Salary: ");
            string password = ValidationHelper.GetValidPass();
            password = encryptionService.Encrypt(password);

            Employee employee = new()
            {
                Id = Guid.NewGuid(),
                FirstName = fname,
                LastName = lname,
                Email = email,
                Phone = phone,
                Salary = salary,
                Password = password,
            };
            SaveEmployee(employee);
        }

        private void SaveEmployee(Employee emp)
        {
            try
            {
                employees = JsonHelper.DeserializeFromJson<List<Employee>>(filePath) ?? new List<Employee>();
                employees.Add(emp);
                JsonHelper.SerializeToJson(filePath, employees);

                Console.WriteLine("Employee saved successfully!");
                Console.WriteLine($"Total employees: {employees.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving employee: {ex.Message}");
            }
        }
        public void DisplayEmployees()
        {
            try
            {
                employees = JsonHelper.DeserializeFromJson<List<Employee>>(filePath) ?? new List<Employee>();
                if (employees.Count == 0)
                {
                    Console.WriteLine("No employees found ");
                    return;
                }

                foreach (var emp in employees)
                {
                    string decryptedPassword = encryptionService.Decrypt(emp.Password);

                    Console.WriteLine($"Name: {emp.FirstName} {emp.LastName}");
                    Console.WriteLine($"Email: {emp.Email}");
                    Console.WriteLine($"Phone: {emp.Phone}");
                    Console.WriteLine($"Salary: {emp.Salary}");
                    Console.WriteLine($"Password: {decryptedPassword}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying employees: {ex.Message}");
            }
        }
    }


}
