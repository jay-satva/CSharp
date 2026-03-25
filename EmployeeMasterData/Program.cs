// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using EmployeeMasterData.Services;
using System.Configuration;

EmployeeService empservice = new();
while (true)
{
    Console.WriteLine("Enter your choice:");
    Console.WriteLine("1. Add new employee");
    Console.WriteLine("2. Remove employee");
    Console.WriteLine("3. Exit");
    int input;
    while (!int.TryParse(Console.ReadLine(), out input))
        Console.WriteLine("Enter valid number");
    switch (input)
    {
        case 1:
            empservice.AddEmployee();
            break;
        case 2:
            empservice.DeleteEmployee();
            break;
        case 3:
            return;
        default:
            Console.WriteLine("Enter valid number");
            break;
    }
}