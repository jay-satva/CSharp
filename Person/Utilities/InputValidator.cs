namespace Person.Utilities
{
    public static class InputValidator
    {
        public static string GetNonEmptyString(string message)
        {
            string input;
            Console.Write(message);
            input = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Name cannot be empty");
                input = Console.ReadLine();

            }
            return input;
        }

        public static int GetPositiveInt(string message)
        {
            int value;

            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out value) && value > 0)
                    return value;
                Console.WriteLine("Enter a valid positive number");
            }
        }
    }
}