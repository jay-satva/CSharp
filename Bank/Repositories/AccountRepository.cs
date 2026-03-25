using Bank.Models;
using Bank.Utils;

namespace Bank.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly string filePath = "Data/accounts.xml";

        public List<Account> GetAllAccounts()
        {
            try
            {
                return XmlHelper.DeserializeFromXml<List<Account>>(filePath) ?? new();
            }
            catch (FileNotFoundException)
            {
                return new List<Account>();
            }
        }

        public Account GetAccountByNumber(Guid accountNumber)
        {
            return GetAllAccounts()
                .FirstOrDefault(a => a.AccountNumber == accountNumber);
        }

        public void AddAccount(Account account)
        {
            var accounts = GetAllAccounts();

            accounts.Add(account);

            XmlHelper.SerializeToXml(filePath, accounts);
        }

        public void UpdateAccount(Account account)
        {
            var accounts = GetAllAccounts();

            var existing = accounts.FirstOrDefault(a => a.AccountNumber == account.AccountNumber);

            if (existing != null)
            {
                accounts.Remove(existing);
                accounts.Add(account);
            }

            XmlHelper.SerializeToXml(filePath, accounts);
        }
    }
}