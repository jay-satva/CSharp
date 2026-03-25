// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.Xml.Linq;

Console.Write("Enter your name: ");
string userName = Console.ReadLine();

Console.WriteLine($"Hello " +userName+ "Do you want to start the quiz? (Y/N)");

string start = Console.ReadLine().ToUpper().Trim();

if (start != "Y")
{
    Console.WriteLine("Quiz cancelled");
    return;
}
//this list will srtore quiz objects
List<Quiz> quiz = new List<Quiz>()
{
    new Quiz { Question = "What is the capital of India", Options = new [] {"Mumbai", "New Delhi", "Chennai", "Kolkata"}, Answer = 'B' },
    new Quiz { Question = "What is the national animal of India", Options = new [] {"Lion", "Tiger", "Bear", "Elephant"}, Answer = 'B' },
    new Quiz { Question = "What is the national sport of India", Options = new [] {"Hockey", "Cricket", "Football", "Basketball"}, Answer = 'A' },
    new Quiz { Question = "What is the national flower of India",  Options = new [] {"Rose", "Hibiscus", "Lotus","Tulip"}, Answer = 'C' },
    new Quiz { Question = "What is the independence year", Options = new [] {"1945", "1946", "1947", "1948"}, Answer = 'C' },
    new Quiz { Question = "What is the financial capital of India", Options = new [] {"Mumbai", "New Delhi", "Chennai", "Kolkata"}, Answer = 'A' },
    new Quiz { Question = "How many states are there in India", Options = new [] {"25", "26", "27", "28"}, Answer = 'D' },
    new Quiz { Question = "How many Union territories in India", Options = new [] {"5", "6", "7", "8"}, Answer = 'D' },
    new Quiz { Question = "What is the population of India", Options = new [] {"1.4B", "1.3B", "1.2B", "1.1B"}, Answer = 'A' },
    new Quiz { Question = "What is national song of India", Options = new [] {"Chak de India", "Vande Mataram", "Jana gan man", "None"}, Answer = 'B' }
};
int score = 0;
foreach (var q in quiz)
{
    Console.WriteLine(q.Question);
    for (int i = 0; i < q.Options.Length; i++)
    {
        Console.WriteLine($"{(char)('A' + i)}. {q.Options[i]}");
    }
    string ans = Console.ReadLine().ToUpper();
    if (ans == q.Answer.ToString())
    {
        Console.WriteLine("Correct!");
        score++;
    }
    else
    {
        Console.WriteLine($"Wrong answer");
    }
}
Console.WriteLine("Final Score is " + score);
if (score >= 7) Console.WriteLine(userName + " you have passed the quiz!");
else Console.WriteLine(userName + " you have failed the quiz!");

public class Quiz
{
    public string Question;
    public string[] Options = [];
    public char Answer;
}
