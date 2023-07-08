using MyBank.Infrastructure;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MyBank.Infrastructure.Data;
using MyBank.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
class Program
{
    static void Main()
    {
        // Register services and implementations
        var services = new ServiceCollection();
        services.AddDbContext<MyBankDbContext>();
        services.AddScoped<IMyBankDbContext>(provider => provider.GetRequiredService<MyBankDbContext>());
        services.AddScoped<IBankingServices, BankingService>();

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        var bankingService = serviceProvider.GetService<IBankingServices>();


        bool quit = false;
        while (!quit)
        {
            ShowMainMenu();

            string choice = Console.ReadLine()?.ToUpper();
            switch (choice)
            {
                case "I":
                    Console.WriteLine("Please enter transaction details in <Date>|<Account>|<Type>|<Amount> format");
                    Console.WriteLine("(or enter blank to go back to main menu):");
                    string inputI = Console.ReadLine();
                    var resposeI = bankingService?.InputTransactions(inputI);
                    if (resposeI.IsValid)
                    {
                        string accountNumber = inputI.Split('|')[1];
                        Console.WriteLine();
                        Console.WriteLine("Account: " + accountNumber);
                        Console.WriteLine("Date     | Txn Id      | Type | Amount |");
                        foreach (var txn in resposeI.Values)
                        {
                            Console.WriteLine($"{txn.Date} | {txn.Id} | {txn.Type}    | {txn.Amount} |");
                        }
                    }
                    else if (resposeI.Message?.Length > 0)
                    {
                        Console.WriteLine(resposeI.Message);
                    }
                    else
                    {
                        return;
                    }

                    break;
                case "D":
                    Console.WriteLine("Please enter interest rules details in <Date>|<RuleId>|<Rate in %> format");
                    Console.WriteLine("(or enter blank to go back to main menu):");

                    string inputD = Console.ReadLine();
                    var resposeD = bankingService?.DefineInterestRules(inputD);
                    if (resposeD.IsValid)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Interest rules:");
                        Console.WriteLine("Date     | RuleId | Rate (%)");
                        foreach (var r in resposeD.Values)
                        {
                            Console.WriteLine($"{r.Date} | {r.RuleId} | {r.Rate}");
                        }
                    }
                    else if (resposeD.Message?.Length > 0)
                    {
                        Console.WriteLine(resposeD.Message);
                    }
                    else
                    {
                        return;
                    }
                    break;
                case "P":
                    Console.WriteLine("Please enter account and month to generate the statement <Account>|<Month>");
                    Console.WriteLine("(or enter blank to go back to main menu):");
                    string inputP = Console.ReadLine();
                    var resposeP = bankingService?.PrintStatement(inputP);
                    if (resposeP.IsValid)
                    {
                        string accountNumber = inputP.Split('|')[0];
                        Console.WriteLine();
                        Console.WriteLine("Account: " + accountNumber);
                        Console.WriteLine("Date     | Txn Id      | Type | Amount | Balance |");
                        foreach (var txn in resposeP.Values)
                        {
                            Console.WriteLine($"{txn.Date} | {txn.Id} | {txn.Type}    | {txn.Amount} | {txn.Balance} |");
                        }
                    }
                    else if (resposeP.Message?.Length > 0)
                    {
                        Console.WriteLine(resposeP.Message);
                    }
                    else
                    {
                        return;
                    }
                    break;
                case "Q":
                    quit = true;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine();
        }


        ShowQuitMenu();
    }
    static void ShowMainMenu()
    {
        Console.WriteLine("Welcome to AwesomeGIC Bank! What would you like to do?");
        Console.WriteLine("[I]nput transactions");
        Console.WriteLine("[D]efine interest rules");
        Console.WriteLine("[P]rint statement");
        Console.WriteLine("[Q]uit");
        Console.Write("> ");
    }
    static void ShowQuitMenu()
    {
        Console.WriteLine("Thank you for banking with AwesomeGIC Bank.");
        Console.WriteLine("Have a nice day!");
    }
}
