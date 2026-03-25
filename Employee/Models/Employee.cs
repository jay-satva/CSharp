namespace EmployeeManagementApp.Models;

public enum Gender { MALE, FEMALE, OTHERS }
public enum Designation { Manager, Director, Supervisor, HR, Admin, User }

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public Gender Gender { get; set; }
    public Designation Designation { get; set; }
    public decimal Salary { get; set; }
}