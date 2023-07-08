using MyBank.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Services.Models
{
    public class TransactionDto
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public TransactionTypes Type { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }

    }
}
