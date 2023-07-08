using MyBank.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Infrastructure.Data.Models
{
    public class Transaction
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string Account { get; set; }
        public TransactionTypes Type { get; set; }
        public decimal Amount { get; set; }
        
    }
}
