using System;
using System.Collections.Generic;
using System.Text;
using Bank.Models;
using System.Collections.Generic;

namespace Bank.Repositories
{

    public interface IAccountRepository
    {
        List<Account> GetAllAccounts();
        Account GetAccountByNumber(Guid accountNumber);
        void AddAccount(Account account);
        void UpdateAccount(Account account);
    }
}
