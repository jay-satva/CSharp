// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using System.IO;
//================================= 1. create file and input text ===================================================

using (StreamWriter stream = new StreamWriter("data.txt"))
{
    stream.WriteLine("Hello there");
    stream.WriteLine("This is file handling");
}

//================================= 2. create file, input text and read text ===================================================

using (StreamReader reader = new StreamReader("data.txt"))
{
    string content = reader.ReadToEnd();
    Console.WriteLine(content);
}

//================================= create file, input text and read text using File ===================================================

string text = "This is using File.writeText";
File.WriteAllText("demo.txt", text);

Console.WriteLine(File.ReadAllText("demo.txt"));

//================================= 3. create file, input array of strings ===================================================

File.WriteAllLines("demo.txt", new[]
{
    "Hello there", "This is array", "Of strings"
});


//================================= 4. append text into existing file ===================================================

File.AppendAllText("demo.txt", "This is appended line");

//================================= 5. read the first line from a file ========================================================

string firstLine = File.ReadAllLines("demo.txt").First();   
Console.WriteLine("First line "+firstLine);

//================================= 6. count the number of lines in a file ===================================================

Console.WriteLine("Total Number of lines: "+File.ReadAllLines("demo.txt").Length);

