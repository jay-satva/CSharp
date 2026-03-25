using System;
using System.Collections.Generic;
using System.Text;

namespace Student.Utils
{
    public static class StringValidator
    {
        public static string GetNonEmptyString(string message)
        {
            string input;
            Console.Write(message);
            input = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Value cannot be empty");
                input = GetNonEmptyString("Enter Name: ");
            }
            return input;
        }
        public static string GetValidRollNo()
        {
            string rollNo = GetNonEmptyString("Enter Roll No: ");
            while (rollNo.Length != 3)
            {
                Console.WriteLine("Roll no must contain 3 digits");
                rollNo = GetNonEmptyString("Enter Roll No: ");
            }
            return rollNo;
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
    }
}
