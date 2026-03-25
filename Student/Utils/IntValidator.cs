using System;
using System.Collections.Generic;
using System.Text;

namespace Student.Utils
{
    public static class IntValidator
    {
        public static int GetPositiveInt(string message)
        {
            int value;
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out value) && value > 0)
                    return value;
                Console.WriteLine("Enter a valid positive number");
            }
        }
        public static int GetValidAge()
        {
            int age = GetPositiveInt("Enter Age: ");
            while (true)
            {
                if (age >= 20 && age <= 30) return age;
                Console.WriteLine("Age must be between 20 and 30 years");
            }
        }
        public static int GetValidSem()
        {
            int sem = GetPositiveInt("Enter Semester");
            while (true)
            {
                if(sem>=1 && sem<=8) return sem;
                Console.WriteLine("Enter valid semester");
            }
        }
        
    }
}
