using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Infrastructure.Data.Models
{
    public class Account
    {
        [Key]
        [Required]
        public string AccountNumber { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public decimal Balance { get; set; }
    }
}
