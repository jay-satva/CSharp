using AutoMapper;
using Bank.DTO;
using Bank.Exceptions;
using Bank.Models;
using Bank.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Bank.Services
{
    public class AccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;

        public AccountService(IAccountRepository accountRepository, IMapper mapper)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
        }

        public void OpenAccount(AccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.HolderName))
                throw new ArgumentNullException("Holder name is required");

            if (dto.Balance < 1000)
                throw new InvalidOperationException("Minimum opening balance is 1000");

            var account = _mapper.Map<Account>(dto);

            account.AccountNumber = Guid.NewGuid();

            account.Transactions.Add(new Transaction
            {
                TransactionDate = DateTime.Now,
                Amount = dto.Balance,
                Type = "Account Opening"
            });

            _accountRepository.AddAccount(account);
        }
        private void PerformTransaction<T>(Guid accountNumber, decimal amount, string type)
        {
            var account = _accountRepository.GetAccountByNumber(accountNumber);

            if (account == null)
                throw new Exception("Account not found");

            if (amount <= 0)
                throw new InvalidOperationException("Amount must be positive");

            if (type == "Withdraw" && account.Balance < amount)
                throw new InsufficientBalanceException("Insufficient funds");

            if (type == "Withdraw")
                account.Balance -= amount;
            else
                account.Balance += amount;

            account.Transactions.Add(new Transaction
            {
                TransactionDate = DateTime.Now,
                Amount = amount,
                Type = type
            });

            _accountRepository.UpdateAccount(account);
        }
        public void Deposit(Guid accountNumber, decimal amount)
        {
            PerformTransaction<Guid>(accountNumber, amount, "Deposit");
        }
        public void Withdraw(Guid accountNumber, decimal amount)
        {
            PerformTransaction<Guid>(accountNumber, amount, "Withdraw");
        }
        public void ApplyMonthlyInterest(Guid accountNumber)
        {
            var account = _accountRepository.GetAccountByNumber(accountNumber);

            decimal rate = decimal.Parse(
                ConfigurationManager.AppSettings["InterestRate"]
            );

            decimal interest = account.Balance * rate;

            account.Balance += interest;

            account.Transactions.Add(new Transaction
            {
                TransactionDate = DateTime.Now,
                Amount = interest,
                Type = "Interest"
            });

            _accountRepository.UpdateAccount(account);
        }
        public AccountDto GetAccount(Guid accountNumber)
        {
            var account = _accountRepository.GetAccountByNumber(accountNumber);

            if (account == null)
                throw new Exception("Account not found");

            return _mapper.Map<AccountDto>(account);
        }
    }
}
