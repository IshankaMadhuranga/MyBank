using Microsoft.EntityFrameworkCore;
using MyBank.Infrastructure.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Infrastructure.Data
{
    public interface IMyBankDbContext
    {
         DbSet<Account> Accounts { get; set; }
         DbSet<InterestRule> InterestRules { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    }
}
