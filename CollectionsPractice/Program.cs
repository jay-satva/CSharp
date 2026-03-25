// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using CollectionsPractice.Model;

Practical1 p1 = new();
Practical2 p2 = new();


while (true)
{
    Console.WriteLine("\nSelect your choice");
    Console.WriteLine("1. Create a new list from a given list of integers where each integer value is added to 2 and the result value is multiplied by 5.");
    Console.WriteLine("2. Group numbers in series of smaller ranges");
    Console.WriteLine("3. Exit");
    string choice = Console.ReadLine();
    switch (choice)
    {
        case "1":
            p1.calculate();
            break;
        case "2":
            p2.Range();
            break;
        case "3":
            return;
        default:
            Console.WriteLine("Invalid choice");
            break;
    }
}