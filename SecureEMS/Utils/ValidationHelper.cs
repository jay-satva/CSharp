using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SecureEMS.Utils
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
        public static string GetValidContact()
        {
            string contact = GetNonEmptyString("Enter Contact No: ");
            while (contact.Length != 10)
            {
                Console.WriteLine("Contact must be 10 digits");
                contact = GetNonEmptyString("Enter Contact No: ");
            }
            return contact;
        }
        public static string GetValidPass()
        {
            string pass = GetNonEmptyString("Enter Password: ");
            while (pass.Length < 6)
            {
                Console.WriteLine("Password must be at least 6 characters");
                pass = GetNonEmptyString("Enter Password: ");
            }
            return pass;
        }
        public static int GetValidSalary(string message)
        {
            int value;
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out value) && value >= 20000 && value <= 100000)
                    return value;
                Console.WriteLine("Enter a valid salary");
            }
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
    }
}
