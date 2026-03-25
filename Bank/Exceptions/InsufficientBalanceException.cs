using System;
using System.Collections.Generic;
using System.Text;

namespace Bank.Exceptions
{
    public class InsufficientBalanceException : Exception
    {
        public InsufficientBalanceException()
            : base("Insufficient balance.")
        {
        }
        public InsufficientBalanceException(string message)
            : base(message)
        {
        }
    }
}
