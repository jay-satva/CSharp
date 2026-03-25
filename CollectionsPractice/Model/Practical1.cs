using System;
using System.Collections.Generic;
using System.Text;

namespace CollectionsPractice.Model
{
    public class Practical1
    {
        List<int> numbers = new();
        public void calculate()
        {
            Console.WriteLine("Enter numbers to populate list");
            Console.WriteLine("Enter any character to stop entering numbers into list");
            while (true)
            {
                int num;
                if (int.TryParse(Console.ReadLine(), out num))
                {
                    numbers.Add(num);
                }
                else
                {
                    break;
                }
            }
            Console.WriteLine("Original list is: ");
            foreach (int num in numbers)
            {
                Console.WriteLine(num);
            }

            List<int> newList = new();
            foreach (int num in numbers)
            {
                newList.Add((num + 2) * 5);
            }

            Console.WriteLine("Calculated list is: ");
            foreach (int num in newList)
            {
                Console.WriteLine(num);
            }
        }
    }
}
