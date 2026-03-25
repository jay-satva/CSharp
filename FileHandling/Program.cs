// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

FileHandling handler = new FileHandling("demo.txt");
handler.AddText("Hello there");
handler.AppendText("This is the appended text");
//handler.AddLines(new[] { "Line 1", "Line 2" });
handler.readText();
handler.ReadFirstLine();
Console.WriteLine("Line Count is "+handler.CountLines());


public class FileHandling
{
    private readonly string Path;
    public FileHandling(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        Path = filePath; 
    }
    public void AddText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text cant be null");
        }
        try
        {
            File.WriteAllText(Path, text);
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error writing to file: " + ex.Message);
        }
    }
    public void AddLines(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            throw new ArgumentException("Lines cannot be null or empty.", nameof(lines));

        try
        {
            File.WriteAllLines(Path, lines);
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error writing lines to file: " + ex.Message);
        }
    }
    public void AppendText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text cant be null");
        }
        try
        {
            File.AppendAllText(Path, text);
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error writing to file: " + ex.Message);
        }
    }
    public void readText()
    {
        try
        {
            if (!File.Exists(Path))
                Console.WriteLine(string.Empty);

            Console.WriteLine("Text: \n"+File.ReadAllText(Path));
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error reading file: " + ex.Message);
            Console.WriteLine(string.Empty);
        }
    }
    public void ReadFirstLine()
    {
        try
        {
            if (!File.Exists(Path))
                Console.WriteLine(string.Empty);

            Console.WriteLine("First line: "+File.ReadAllLines(Path).First());
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error reading first line: " + ex.Message);
            Console.WriteLine(string.Empty);
        }
    }
    public int CountLines()
    {
        try
        {
            if (!File.Exists(Path))
                return 0;

            return File.ReadAllLines(Path).Length;
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error counting lines: " + ex.Message);
            return 0;
        }
    }
}
