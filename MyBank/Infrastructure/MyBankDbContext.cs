using Microsoft.EntityFrameworkCore;
using MyBank.Infrastructure.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Infrastructure.Data
{
    [ExcludeFromCodeCoverage]
    public class MyBankDbContext:DbContext,IMyBankDbContext
    {

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // in memory database used for simplicity, change to a real db for production applications
            options.UseInMemoryDatabase("MyBankDb");
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<InterestRule> InterestRules { get; set; }
    }
}
