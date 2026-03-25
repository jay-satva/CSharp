// See https://aka.ms/new-console-template for more information

using System;
public class Hello
{
    public static void Main(string[] args)
    {
        string name = "John Doe";
        int age = 21;
        char gender = 'M';
        //implicit typecasting
        double ageDouble = age;
        //explicit typecasting - done by placing the types inside the bracket
        int myInt = (int)ageDouble;
        //user input
        //Console.WriteLine("Enter username");
        //string username = Console.ReadLine();
        int x = 23, y = 24, z = 25;
        int max = Math.Max(x, y);
        Console.WriteLine(max);
        Console.WriteLine(Math.Sqrt(z));
        Console.WriteLine(Math.Abs(-10));
        Console.WriteLine(Math.Round(10.98));

        int charPos = name.IndexOf("D");
        string lastName = name.Substring(charPos);
        Console.WriteLine(lastName);
        Console.WriteLine("It\'s alright");
        //Console.WriteLine("Hello "+username + " your age is "+age +" years");
    }
}