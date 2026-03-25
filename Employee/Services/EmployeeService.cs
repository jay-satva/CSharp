using EmployeeManagementApp.Models;
using EmployeeManagementApp.Utilities;
using System.Text.Json;

namespace EmployeeManagementApp.Services;

public class EmployeeService
{
    private readonly string _filePath;
    private List<Employee> _employees;

    public EmployeeService(string filePath)
    {
        _filePath = filePath;
        _employees = File.Exists(_filePath) && !string.IsNullOrEmpty(File.ReadAllText(_filePath))
                     ? JsonSerializer.Deserialize<List<Employee>>(File.ReadAllText(_filePath))!
                     : new List<Employee>();
    }

    private void SaveData()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_employees, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void AddEmployee()
    {
        var emp = new Employee();
        emp.Id = _employees.Count > 0 ? _employees.Max(e => e.Id) + 1 : 1;

        // First Name
        Console.Write("Enter First Name: ");
        string firstName = Console.ReadLine()!;
        while (!firstName.IsRequired())
        {
            Console.WriteLine("First Name is required");
            firstName = Console.ReadLine()!;
        }
        emp.FirstName = firstName;

        // Last Name
        Console.Write("Enter Last Name: ");
        string lastName = Console.ReadLine()!;
        while (!lastName.IsRequired())
        {
            Console.WriteLine("Last Name is required");
            lastName = Console.ReadLine()!;
        }
        emp.LastName = lastName;

        // Gender
        Console.Write("Enter Gender (MALE/FEMALE/OTHERS): ");
        Gender gender;
        string gInput = Console.ReadLine()!;
        while (!Enum.TryParse(gInput, true, out gender))
        {
            Console.WriteLine("Invalid gender");
            gInput = Console.ReadLine()!;
        }
        emp.Gender = gender;

        // Email
        Console.Write("Enter Email: ");
        string email = Console.ReadLine()!;
        while (string.IsNullOrWhiteSpace(email) || _employees.Any(e => e.Email == email))
        {
            Console.WriteLine("Email is required and must be unique");
            email = Console.ReadLine()!;
        }
        emp.Email = email;

        // Phone
        Console.Write("Enter Phone Number: ");
        string phone = Console.ReadLine()!;
        while (!phone.IsValidPhone())
        {
            Console.WriteLine("Phone must be 10 digits");
            phone = Console.ReadLine()!;
        }
        emp.PhoneNumber = phone;

        // Designation
        Console.Write("Enter Designation (Manager/Director/Supervisor/HR/Admin/User): ");
        Designation desig;
        string dInput = Console.ReadLine()!;
        while (!Enum.TryParse(dInput, true, out desig))
        {
            Console.WriteLine("Invalid designation");
            dInput = Console.ReadLine()!;
        }
        emp.Designation = desig;

        // Salary
        Console.Write("Enter Salary between 10000 and 50000: ");
        decimal salary;
        while (!decimal.TryParse(Console.ReadLine(), out salary) || !salary.IsValidSalary())
        {
            Console.WriteLine("Salary must be between 10000 and 50000");
        }
        emp.Salary = salary;

        _employees.Add(emp);
        SaveData();
        Console.WriteLine("Employee added successfully");
    }

    public void UpdateEmployee()
    {
        Console.Write("Enter Employee Id to update: ");
        int id;
        while (!int.TryParse(Console.ReadLine(), out id))
            Console.WriteLine("Enter a valid Id");

        var emp = _employees.FirstOrDefault(e => e.Id == id);
        if (emp == null)
        {
            Console.WriteLine("Employee not found");
            return;
        }

        Console.Write("Enter First Name to update or leave blank: ");
        string firstName = Console.ReadLine()!;
        if (firstName.IsRequired())
            emp.FirstName = firstName;

        Console.Write("Enter Last Name to update or leave blank: ");
        string lastName = Console.ReadLine()!;
        if (lastName.IsRequired())
            emp.LastName = lastName;

        Console.Write("Enter Email to update or leave blank: ");
        string email = Console.ReadLine()!;
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (_employees.Any(e => e.Email == email && e.Id != id))
                Console.WriteLine("Email already exists, not updated");
            else
                emp.Email = email;
        }

        Console.Write("Enter Phone Number to update or leave blank: ");
        string phone = Console.ReadLine()!;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            if (phone.IsValidPhone())
                emp.PhoneNumber = phone;
            else
                Console.WriteLine("Invalid phone, not updated");
        }

        Console.Write("Enter Designation to update or leave blank: ");
        string dInput = Console.ReadLine()!;
        if (!string.IsNullOrWhiteSpace(dInput) && Enum.TryParse(dInput, true, out Designation desig))
            emp.Designation = desig;

        Console.Write("Enter Salary to update or leave blank: ");
        string salaryInput = Console.ReadLine()!;
        if (!string.IsNullOrWhiteSpace(salaryInput) && decimal.TryParse(salaryInput, out decimal salary) && salary.IsValidSalary())
            emp.Salary = salary;

        SaveData();
        Console.WriteLine("Employee updated successfully");
    }

    public void DeleteEmployee()
    {
        Console.Write("Enter Employee Id to delete: ");
        int id;
        while (!int.TryParse(Console.ReadLine(), out id))
            Console.WriteLine("Enter a valid Id");

        var emp = _employees.FirstOrDefault(e => e.Id == id);
        if (emp == null)
        {
            Console.WriteLine("Employee not found");
            return;
        }

        _employees.Remove(emp);
        SaveData();
        Console.WriteLine("Employee deleted successfully");
    }

    public void ShowAllEmployees()
    {
        if (!_employees.Any())
        {
            Console.WriteLine("No employees to show");
            return;
        }

        foreach (var emp in _employees)
        {
            Console.WriteLine($"Id: {emp.Id}, Name: {emp.FirstName} {emp.LastName}, Email: {emp.Email}, Phone: {emp.PhoneNumber}, Salary: {emp.Salary}, Designation: {emp.Designation}");
        }
    }

    public void ShowEmployeeById()
    {
        Console.Write("Enter Employee Id to show: ");
        int id;
        while (!int.TryParse(Console.ReadLine(), out id))
            Console.WriteLine("Enter a valid Id");

        var emp = _employees.FirstOrDefault(e => e.Id == id);
        if (emp == null)
        {
            Console.WriteLine("Employee not found");
            return;
        }

        Console.WriteLine($"Id: {emp.Id}, Name: {emp.FirstName} {emp.LastName}, Email: {emp.Email}, Phone: {emp.PhoneNumber}, Salary: {emp.Salary}, Designation: {emp.Designation}");
    }

    public void ShowEmployeeByEmail()
    {
        Console.Write("Enter Employee Email to show: ");
        string email = Console.ReadLine()!;
        var emp = _employees.FirstOrDefault(e => e.Email == email);

        if (emp == null)
        {
            Console.WriteLine("Employee not found");
            return;
        }

        Console.WriteLine($"Id: {emp.Id}, Name: {emp.FirstName} {emp.LastName}, Email: {emp.Email}, Phone: {emp.PhoneNumber}, Salary: {emp.Salary}, Designation: {emp.Designation}");
    }
}