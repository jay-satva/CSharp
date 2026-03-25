using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using EmployeeMasterData.Models;
namespace EmployeeMasterData.Utils
{
    public static class ValidationHelper
    {
        public static string GetNonEmptyString(string message)
        {
            string input;
            Console.Write(message);
            input = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Value cannot be empty");
                input = GetNonEmptyString("Enter Value Again: ");
            }
            return input;
        }

        public static string GetAlphabeticString(string message)
        {
            string input;
            Regex regex = new Regex("^[a-zA-Z ]+$");
            while (true)
            {
                input = GetNonEmptyString(message);
                if (regex.IsMatch(input))
                    return input;
                Console.WriteLine("Only alphabetic characters are allowed.");
            }
        }
        public static string GetValidContact()
        {
            string contact = GetNonEmptyString("Enter Contact No: ");
            while (!Regex.IsMatch(contact, @"^\d{10}$"))
            {
                Console.WriteLine("Contact must be 10 digits");
                contact = GetNonEmptyString("Enter Contact No: ");
            }
            return contact;
        }
        public static string GetValidEmail()
        {
            string email;
            Regex regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

            while (true)
            {
                email = GetNonEmptyString("Enter email: ");
                if (regex.IsMatch(email))
                    return email;
                Console.WriteLine("Enter a valid email address ");
            }
        }
        public static int GetValidSalary(string message)
        {
            int value;
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out value) && value >= 10000)
                    return value;
                Console.WriteLine("Enter a valid salary more than 10000");
            }
        }
        
        public static DateOnly GetValidDOJ(string message, DateOnly dob)
        {
            DateOnly date;
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            while (true)
            {
                Console.Write(message);
                string input = Console.ReadLine();

                if (!DateOnly.TryParse(input, out date))
                {
                    Console.WriteLine("Enter a valid date in format YYYY-MM-DD");
                    continue;
                }

                if (date > today)
                {
                    Console.WriteLine("Joining date cannot be in the future.");
                    continue;
                }

                if (date < dob)
                {
                    Console.WriteLine("Joining date cannot be before DOB.");
                    continue;
                }

                return date;
            }
        }
        public static DateOnly GetValidDOB(string message)
        {
            DateOnly date;
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly minDOB = today.AddYears(-16); 
            while (true)
            {
                Console.Write(message);
                string input = Console.ReadLine();

                if (!DateOnly.TryParse(input, out date))
                {
                    Console.WriteLine("Enter a valid date in format YYYY-MM-DD");
                    continue;
                }

                if (date > today)
                {
                    Console.WriteLine("DOB cannot be in the future.");
                    continue;
                }

                if (date > minDOB)
                {
                    Console.WriteLine("Employee must be at least 16 years old.");
                    continue;
                }
                return date;
            }
        }

        public static string GetValidGender()
        {
            string gender;
            while (true)
            {
                Console.Write("Enter Gender (M/F): ");
                gender = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(gender) &&
                    (gender.Equals("M", StringComparison.OrdinalIgnoreCase) ||
                     gender.Equals("F", StringComparison.OrdinalIgnoreCase)))
                {
                    return gender.ToUpper();
                }

                Console.WriteLine("Invalid input, enter 'M' or 'F'");
            }
        }
        public static int GetValidPincode()
        {
            int pincode;
            while (true)
            {
                Console.Write("Enter Pincode: ");
                if (int.TryParse(Console.ReadLine(), out pincode) && pincode >= 100000 && pincode <= 999999)
                    return pincode;
                Console.WriteLine("Enter a valid 6 digit pincode");
            }
        }
        public static Department GetValidDepartment()
        {
            while (true)
            {
                Console.WriteLine("Select Department:");
                foreach (var dept in Enum.GetValues(typeof(Department)))
                {
                    Console.WriteLine($"{(int)dept} - {dept}");
                }
                Console.Write("Enter department number: ");
                if (int.TryParse(Console.ReadLine(), out int deptNum) && Enum.IsDefined(typeof(Department), deptNum))
                    return (Department)deptNum;
                Console.WriteLine("Invalid department number, try again.");
            }
        }
        public static Guid GetValidGuid(string message)
        {
            Guid id;
            while (true)
            {
                Console.Write(message);
                if (Guid.TryParse(Console.ReadLine(), out id))
                    return id;
                Console.WriteLine("Enter a valid GUID");
            }
        }
    }
}
