
using EmployeeManagementApp.Models;
using EmployeeManagementApp.Services;
using EmployeeManagementApp.Utilities;
using System.Configuration;

string pinFromConfig = ConfigurationManager.AppSettings["AppPIN"];

Console.Write("Enter PIN to run application: ");
string enteredPin = Console.ReadLine();

if (enteredPin != pinFromConfig)
{
    Console.WriteLine("Invalid PIN. Program ends");
    return;
}

var service = new EmployeeService("employees.json");
bool running = true;

while (running)
{
    Console.WriteLine("Enter the desired operation");
    Console.WriteLine("1. Add Employee");
    Console.WriteLine("2. Update Employee");
    Console.WriteLine("3. Delete Employee");
    Console.WriteLine("4. Show All Employees");
    Console.WriteLine("5. Show Employee By Id");
    Console.WriteLine("6. Show Employee By Email");
    Console.WriteLine("7. Exit");

    int input;
    while (!int.TryParse(Console.ReadLine(), out input))
        Console.WriteLine("Enter a valid number");

    switch (input)
    {
        case 1:
            service.AddEmployee();
            break;
        case 2:
            service.UpdateEmployee();
            break;
        case 3:
            service.DeleteEmployee();
            break;
        case 4:
            service.ShowAllEmployees();
            break;
        case 5:
            service.ShowEmployeeById();
            break;
        case 6:
            service.ShowEmployeeByEmail();
            break;
        case 7:
            running = false;
            break;
        default:
            Console.WriteLine("Invalid operation entered");
            break;
    }

    Console.WriteLine();
}