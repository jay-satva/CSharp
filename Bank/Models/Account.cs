using System;
using System.Collections.Generic;
using System.Text;

namespace Bank.Models
{
    public class Account
    {
        public Guid AccountNumber { get; set; }
        public string HolderName { get; set; }
        public decimal Balance { get; set; } = 1000;
        public List<Transaction> Transactions { get; set; } = new();
    }
}
