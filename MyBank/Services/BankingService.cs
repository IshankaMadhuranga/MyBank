using Microsoft.EntityFrameworkCore;
using MyBank.Infrastructure.Data;
using MyBank.Infrastructure.Data.Models;
using MyBank.Infrastructure.Models;
using MyBank.Services.Models;
using System.Data;
using System.Globalization;
using System.Security.Principal;

namespace MyBank.Infrastructure
{
    public class BankingService : IBankingServices
    {
        private readonly IMyBankDbContext _context;

        public BankingService(IMyBankDbContext dbContext)
        {
            _context = dbContext;
        }

        public ReturnDto<List<TransactionDto>> PrintStatement(string input)
        {
            // Assum year as current year
            var output = ValidateInput<List<TransactionDto>>(input, 2);
            if (!string.IsNullOrEmpty(output.Message))
            {
                return output;
            }

            string[] parts = input.Split('|');
            string accountNumber = parts[0];
            string month = parts[1];

            Account account = _context.Accounts.FirstOrDefault(u => u.AccountNumber == accountNumber);
            if (account == null)
            {
                output.Message = "Account not found.";
                return output;
            }

            decimal interest = CalculateInterest(account, month);
            if (interest > 0)
            {
                string lastDayOfMonth = new DateTime(DateTime.Now.Year, int.Parse(month), DateTime.DaysInMonth(DateTime.Now.Year, int.Parse(month))).ToString("yyyyMMdd");
                Transaction interestTransaction = new Transaction
                {
                    Date = lastDayOfMonth,
                    Account = accountNumber,
                    Type = TransactionTypes.I,
                    Amount = interest,
                    Id = "           "
                };

                Transaction rmTransaction = account.Transactions.FirstOrDefault(r => r.Type == TransactionTypes.I && r.Date == lastDayOfMonth);
                if (rmTransaction != null)
                {
                    account.Transactions.Remove(rmTransaction);
                }
                account.Transactions.Add(interestTransaction);
                _context.SaveChangesAsync();
            }
            List<TransactionDto> transactions = new List<TransactionDto>();

            List<Transaction> trns = account.Transactions.Where(txn => DateTime.ParseExact(txn.Date, "yyyyMMdd", null).Month == int.Parse(month)).ToList();
            foreach (var txn in trns)
            {

                var displayObj = new TransactionDto
                {
                    Id = txn.Id,
                    Date = txn.Date,
                    Type = txn.Type,
                    Amount = txn.Amount,
                    Balance = GetBalance(account, txn.Date),
                };
                transactions.Add(displayObj);
            }
            output.IsValid = true;
            output.Values = transactions;
            return output;

        }

        public ReturnDto<List<InterestRule>> DefineInterestRules(string input)
        {
            var output = ValidateInput<List<InterestRule>>(input, 3);
            if (!string.IsNullOrEmpty(output.Message))
            {
                return output;
            }

            string[] parts = input.Split('|');
            string date = parts[0];
            string ruleId = parts[1];
           
            decimal rate;
            if (!decimal.TryParse(parts[2], out rate) || rate <= 0 || rate >= 100)
            {
                output.Message = "Invalid rate. Please enter a rate between 0 and 100.";
                return output;
            }
            if (!IsValidDate(date))
            {
                output.Message = "Invalid date entered.";
                return output;
            };
            InterestRule rule = new InterestRule
            {
                Date = date,
                RuleId = ruleId,
                Rate = rate
            };

            _context.InterestRules.RemoveRange(_context.InterestRules.Where(r => r.Date == date));
            _context.InterestRules.Add(rule);
            _context.SaveChangesAsync();

            output.IsValid = true;
            output.Values = _context.InterestRules.OrderBy(rul => DateTime.ParseExact(rul.Date, "yyyyMMdd", null)).ToList();
            return output;
        }

      

        public ReturnDto<List<Transaction>> InputTransactions(string input)
        {
            var output = ValidateInput<List<Transaction>>(input, 4);
            if (!string.IsNullOrEmpty(output.Message))
            {
                return output;
            }

            string[] parts = input.Split('|');
           

            string date = parts[0];
            string accountNumber = parts[1];
            TransactionTypes type = (TransactionTypes)Enum.Parse(typeof(TransactionTypes), parts[2].ToUpper());
            
            decimal amount;
            if (!decimal.TryParse(parts[3], out amount) || amount <= 0)
            {
                output.Message = "Invalid amount. Please enter a positive number.";
                return output;
            }

            if (!IsValidDate(date))
            {
                output.Message = "Invalid date entered.";
                return output;
            };
            Account account = _context.Accounts.FirstOrDefault(u => u.AccountNumber == accountNumber);
            if (account == null)
            {
                account = new Account { AccountNumber = accountNumber };
                _context.Accounts.Add(account);
                _context.SaveChangesAsync();
            }

            if (account.Transactions.Count == 0 && type == TransactionTypes.W)
            {
                output.Message = "First transaction on an account cannot be a withdrawal.";
                return output;
            }

            if (type == TransactionTypes.W && account.Balance < amount)
            {
                output.Message = "Insufficient balance for withdrawal.";
                return output;
            }

            Transaction transaction = new Transaction
            {
                Date = date,
                Account = accountNumber,
                Type = type,
                Amount = amount,
                Id = GenerateTransactionId(date, account)
            };

            account.Transactions.Add(transaction);
            account.Balance += type == TransactionTypes.D ? amount : -amount;

            output.IsValid = true;
            output.Values = account.Transactions;
            return output;


        }

        private decimal CalculateInterest(Account account, string month)
        {
            decimal interest = 0;
            DateTime startDate = new DateTime(DateTime.Now.Year, int.Parse(month), 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1); //End date for calculation
            List<InterestRule> rules = _context.InterestRules.Where(rul => DateTime.ParseExact(rul.Date, "yyyyMMdd", null) <= endDate)
                                       .OrderBy(rul => DateTime.ParseExact(rul.Date, "yyyyMMdd", null)).ToList();
            for (var i = 0; i < rules.Count; i++)
            {
                DateTime ruleStartDate = DateTime.ParseExact(rules[i].Date, "yyyyMMdd", null);
                DateTime ruleEndDate = i == rules.Count - 1 ? endDate : DateTime.ParseExact(rules[i + 1].Date, "yyyyMMdd", null).AddDays(-1);
                DateTime calculationStartDate = startDate > ruleStartDate ? startDate : ruleStartDate;
                DateTime calculationEndDate = endDate < ruleEndDate ? endDate : ruleEndDate;
                decimal numDays = (calculationEndDate - calculationStartDate).Days + 1;
                decimal balance = GetBalance(account, calculationStartDate.ToString("yyyyMMdd"));
                decimal ruleInterest = (balance * (rules[i].Rate / 100)) * (numDays / 365);
                interest += ruleInterest;
            }

            return Math.Round(interest, 2);
        }
        private string GenerateTransactionId(string date, Account account)
        {
            int count = 1;

            foreach (var txn in account.Transactions)
            {
                if (txn.Date == date && txn.Id != null)
                {
                    int txnCount = int.Parse(txn.Id.Split('-')[1]);
                    if (txnCount >= count)
                    {
                        count = txnCount + 1;
                    }
                }
            }
            return $"{date}-{count:D2}";
        }

        private decimal GetBalance(Account account, string date)
        {
            decimal balance = 0;
            var trns = account.Transactions.Where(trn => DateTime.ParseExact(trn.Date, "yyyyMMdd", null) <= DateTime.ParseExact(date, "yyyyMMdd", null))
                                       .OrderBy(trn => DateTime.ParseExact(trn.Date, "yyyyMMdd", null));
            foreach (var txn in trns)
            {
                balance += txn.Type == TransactionTypes.W ? -txn.Amount : txn.Amount;
            }
            return balance;
        }
        private Boolean IsValidDate(string inputDate)
        {
            DateTime result;
            return DateTime.TryParseExact(
                     inputDate,
                     "yyyyMMdd",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out result);
        }

        private ReturnDto<T> ValidateInput<T>(string input, int expectedLength)
        {
            var output = new ReturnDto<T>
            {
                IsValid = false,
            };

            if (string.IsNullOrWhiteSpace(input) || input.Count(c => c == '|') != expectedLength - 1)
            {
                output.Message = "Invalid input format. Please try again.";
                return output;
            }

            return output;
        }
      
    }

}