// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using Person.Models;
using Person.Services;
using Person.Utilities;

CityService cs = new();
PersonService ps = new();

while (true)
{
    Console.WriteLine("\n1. Add City");
    Console.WriteLine("2. Add Person");
    Console.WriteLine("3. Show Persons");
    Console.WriteLine("4. Show Available Cities");
    Console.WriteLine("5. Exit");
    Console.Write("Choose option ");
    string choice = Console.ReadLine();
    switch (choice)
    {
        case "1":
            cs.AddCity();
            break;
        case "2":
            ps.AddPerson(cs.GetCities());
            break;
        case "3":
            ps.ShowPersons();  
            break;
        case "4":
            cs.ShowCity();
            break;
        case "5":
            return;
        default:
            Console.WriteLine("Invalid choice");
            break;
    }
}
