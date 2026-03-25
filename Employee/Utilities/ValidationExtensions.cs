namespace EmployeeManagementApp.Utilities;

public static class ValidationExtensions
{
    public static bool IsRequired(this string input) => !string.IsNullOrWhiteSpace(input);
    public static bool IsValidPhone(this string input) => !string.IsNullOrWhiteSpace(input) && input.Length == 10;
    public static bool IsValidSalary(this decimal salary) => salary >= 10000 && salary <= 50000;
}