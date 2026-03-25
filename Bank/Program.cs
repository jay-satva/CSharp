using AutoMapper;
using Bank.DTO;
using Bank.Mappings;
using Bank.Repositories;
using Bank.Services;

var mapperConfig = new MapperConfiguration(config =>
{
    config.AddProfile<AutoMapperProfile>();
});

IMapper mapper = mapperConfig.CreateMapper();

IAccountRepository repository = new AccountRepository();
AccountService service = new AccountService(repository, mapper);

while (true)
{
    Console.WriteLine("\nChoose desired operation");
    Console.WriteLine("1. Open Account");
    Console.WriteLine("2. Deposit Money");
    Console.WriteLine("3. Withdraw Money");
    Console.WriteLine("4. View Account");
    Console.WriteLine("5. Apply Monthly Interest");
    Console.WriteLine("0. Exit");

    Console.Write("Choose option: ");

    int choice = int.Parse(Console.ReadLine());

    try
    {
        switch (choice)
        {
            case 1:
                Console.Write("Enter holder name: ");
                string name = Console.ReadLine();

                Console.Write("Initial deposit: ");
                decimal balance = decimal.Parse(Console.ReadLine());

                AccountDto dto = new AccountDto
                {
                    HolderName = name,
                    Balance = balance
                };

                service.OpenAccount(dto);

                Console.WriteLine("Account created successfully");
                break;

            case 2:
                Console.Write("Enter account number: ");
                Guid depositId = Guid.Parse(Console.ReadLine());

                Console.Write("Enter amount: ");
                decimal deposit = decimal.Parse(Console.ReadLine());

                service.Deposit(depositId, deposit);

                Console.WriteLine("Deposit successful");
                break;

            case 3:
                Console.Write("Enter account number: ");
                Guid withdrawId = Guid.Parse(Console.ReadLine());

                Console.Write("Enter amount: ");
                decimal withdraw = decimal.Parse(Console.ReadLine());

                service.Withdraw(withdrawId, withdraw);

                Console.WriteLine("Withdrawal successful!");
                break;

            case 4:
                Console.Write("Enter account number: ");
                Guid id = Guid.Parse(Console.ReadLine());

                var account = service.GetAccount(id);

                Console.WriteLine($"\nHolder: {account.HolderName}");
                Console.WriteLine($"Balance: {account.Balance}");

                Console.WriteLine("\nTransactions:");

                foreach (var t in account.Transactions)
                {
                    Console.WriteLine($"{t.TransactionDate} | {t.Type} | {t.Amount}");
                }

                break;

            case 5:
                Console.Write("Enter account number: ");
                Guid interestId = Guid.Parse(Console.ReadLine());

                service.ApplyMonthlyInterest(interestId);

                Console.WriteLine("Interest applied.");
                break;

            case 0:
                return;

            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}