using System;

Console.Title = "Guess A Number";

int value = Random.Shared.Next(1, 101);
while (true)
{
    Console.Write("Guess a number (1-100): ");
    bool valid = int.TryParse((await Console.ReadLine() ?? "").Trim(), out int input);
    if (!valid) Console.WriteLine("Invalid.");
    else if (input == value) break;
    else Console.WriteLine($"Incorrect. Too {(input < value ? "Low" : "High")}.");
}
Console.WriteLine("You guessed it!");
Console.Write("Press any key to exit...");
await Console.ReadKey(true);
Console.Clear();
Console.Write("Guess A Number was closed.");