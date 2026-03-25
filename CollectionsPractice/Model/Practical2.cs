using System;
using System.Collections.Generic;
using System.Text;

namespace CollectionsPractice.Model
{
    public class Practical2
    {
        public void Range()
        {
            List<int> numbers = new();
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
            int start = numbers[0];

            for (int i = 0; i < numbers.Count; i++)
            {
                // if last element or sequence breaks
                if (i == numbers.Count - 1 || numbers[i] + 1 != numbers[i + 1])
                {
                    if (start == numbers[i])
                    {
                        Console.Write(start);
                    }
                    else
                    {
                        Console.Write($"{start}-{numbers[i]}");
                    }

                    if (i != numbers.Count - 1)
                    {
                        Console.Write(", ");
                    }

                    if (i < numbers.Count - 1)
                    {
                        start = numbers[i + 1];
                    }
                }
            }
        }
    }
}
