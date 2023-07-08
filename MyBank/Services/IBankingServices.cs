using MyBank.Infrastructure.Data.Models;
using MyBank.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Infrastructure
{
    public interface IBankingServices
    {
        ReturnDto<List<Transaction>> InputTransactions(string input);
        ReturnDto<List<InterestRule>> DefineInterestRules(string input);
        ReturnDto<List<TransactionDto>> PrintStatement(string input);

    }
}
