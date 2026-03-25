// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using SecureEMS.Services;
using System.Configuration;
using System.Text;

var key = ConfigurationManager.AppSettings["EncryptionKey"];
var iv = ConfigurationManager.AppSettings["EncryptionIV"];

byte[] keyBytes = Encoding.UTF8.GetBytes(key);
byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

IEncryptionService encryptionService = new EncryptionService(keyBytes, ivBytes);
EmployeeServices employeeService = new EmployeeServices(encryptionService);

while (true)
{
    Console.WriteLine("Enter your choice");
    Console.WriteLine("1. Add Employee");
    Console.WriteLine("2. Retrieve All Employees");
    Console.WriteLine("3. Exit");
    //Console.WriteLine("3. Add Employee");
    int input;
    while (!int.TryParse(Console.ReadLine(), out input))
        Console.WriteLine("Enter a valid number");

    switch (input)
    {
        case 1:
            employeeService.AddEmployee();
            break;
        case 2:
            employeeService.DisplayEmployees();
            break;
        case 3:
            return;
        default:
            break;
    }
}