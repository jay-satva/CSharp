using System;
using System.Collections.Generic;
using System.Text;

namespace Bank.DTO
{
    public class AccountDto
    {
        public Guid AccountNumber { get; set; }
        public string HolderName { get; set; }
        public decimal Balance { get; set; }
        public List<TransactionDto> Transactions { get; set; } = new();
    }
}
