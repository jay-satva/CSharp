// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
Console.WriteLine("Hey, Enter your name");
string name = Console.ReadLine();
Console.WriteLine("Welcome " + name);
int score = 0, wrong = 0;
string[,] questions;
questions = new string[,]
{
    {"What is the capital of India", "A. Mumbai", "B. New Delhi", "C. Chennai", "D. Kolkata", "B"},
    {"What is the national sport of India", "A. Hockey", "B. Cricket", "C. Football", "D. Basketball", "A"},
    {"What is the national animal of India", "A. Lion", "B. Tiger", "C. Bear", "D. Elephant", "B"},
    {"What is the national flower of India", "A. Rose", "B. Hibiscus", "C. Lotus", "D. Sunflower", "C"},
    {"What is the independence year", "A. 1945", "B. 1946", "C. 1947", "D. 1948", "C"},
    {"What is the financial capital of India", "A. Mumbai", "B. New Delhi", "C. Chennai", "D. Kolkata", "A"},
    {"How many states are there in India", "A. 25", "B. 26", "C. 27", "D. 28", "D"},
    {"How many Union territories in India", "A. 6", "B. 7", "C. 8", "D. 9", "C"},
    {"What is the population of India", "A. 1.4B", "B. 1.3B", "C. 1.2B", "D. 1.1B", "A"},
    {"What is national song of India", "A. Chak de India", "B. Vande Mataram", "C. Jana gan man", "D. None", "B"},
};
int[] randomArr = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
Console.WriteLine("Shall we start the quiz? Type yes or no");
string reply = (Console.ReadLine()).ToLower().Trim();
Random random = new Random();
switch (reply)
{
    case "yes":
        int j = 10;
        //for (int j = 0; j < questions.GetLength(0); j++)
        while(j>0)
        {
            int i = random.Next(10);
            if (randomArr[i] == 10) continue;
            randomArr[i]=10;
            Console.WriteLine(questions[i, 0]);
            Console.WriteLine(questions[i, 1]);
            Console.WriteLine(questions[i, 2]);
            Console.WriteLine(questions[i, 3]);
            Console.WriteLine(questions[i, 4]);
            string ans = (Console.ReadLine()).ToUpper();
            if (ans.Trim() == questions[i, 5])
            {
                score += 1;
                Console.WriteLine("Correct!");
                j--;
            }
            else
            {
                wrong += 1;
                Console.WriteLine("incorrect!");
                j--;
            }
        }
        Console.WriteLine("Final Score is " + score);
        if (score >= 7) Console.WriteLine(name + " you have passed the quiz!");
        else Console.WriteLine(name + " you have failed the quiz!");
        break;
        case "no":
            Console.WriteLine("Quiz exitted");
        break;

        default: 
            Console.WriteLine("Invalid Operation"); 
        break;
}

