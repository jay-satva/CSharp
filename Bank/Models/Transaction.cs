using System;
using System.Collections.Generic;
using System.Text;

namespace Bank.Models
{
    public class Transaction
    {
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
    }
}
