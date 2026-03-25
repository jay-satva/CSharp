// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using Student.Models;
using Student.Services;
using Student.Utils;

StudentService ss = new();
AwardService award = new();

while (true)
{
    Console.WriteLine("Enter the desired operation");
    Console.WriteLine("1. Student Admission");
    Console.WriteLine("2. Remove Student");
    Console.WriteLine("3. Add Student Award");
    Console.WriteLine("4. Show All Students");
    Console.WriteLine("5. Show Student By Id");
    Console.WriteLine("6. Exit");
    int input;
    while (!int.TryParse(Console.ReadLine(), out input))
        Console.WriteLine("Enter a valid number");

    switch (input)
    {
        case 1:
            ss.AddStudent();
            break;
        case 2:
            ss.RemoveStudent();
            break;
        case 3:
            award.AddAward();
            break;
        case 4:
            ss.ShowStudent(); 
            break;
        case 5:
            ss.ShowStudentByID();
            break;
        case 6:
            return;
        default:
            Console.WriteLine("Invalid input");
            break;
    }
}