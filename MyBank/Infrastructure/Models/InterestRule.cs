using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Infrastructure.Data.Models
{
    public class InterestRule
    {
        [Key]
        public string RuleId { get; set; }
        public string Date { get; set; }
        public decimal Rate { get; set; }
    }
}
