
using Microsoft.EntityFrameworkCore;
using Moq;
using MyBank.Infrastructure;
using MyBank.Infrastructure.Data;
using MyBank.Infrastructure.Data.Models;
using MyBank.Infrastructure.Models;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace MyBank.Tests
{
    public class BankingServiceTest
    {
        [Fact]
        public void PrintStatement_ValidInput_ReturnsValidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var service = new BankingService(mockDbContext.Object);

            var accountNumber = "ACC001";
            var month = "07";
            var input = $"{accountNumber}|{month}";

            // Mock the necessary data in the DbContext for the account and transactions
            var mockAccount = new Account { AccountNumber = accountNumber };
            var mockTransaction = new Transaction
            {
                Date = "20230701",
                Account = accountNumber,
                Type = TransactionTypes.D,
                Amount = 100,
                Id = "20230701-1"
            };
            mockAccount.Transactions.Add(mockTransaction);
            mockDbContext.Setup(m => m.Accounts).Returns(MockDbSet<Account>(mockAccount));
           
            var mockRule = new InterestRule { Date="20230701",RuleId="RUL001",Rate=5 };
            mockDbContext.Setup(m => m.InterestRules).Returns(MockDbSet<InterestRule>(mockRule));

            mockDbContext.Setup(m => m.SaveChangesAsync(new CancellationToken())).Verifiable();

            // Act
            var result = service.PrintStatement(input);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.Message);
            Assert.NotNull(result.Values);
            Assert.Equal(2,result.Values.Count);

            var transaction = result.Values[0];
            Assert.Equal(mockTransaction.Id, transaction.Id);
            Assert.Equal(mockTransaction.Date, transaction.Date);
            Assert.Equal(mockTransaction.Type, transaction.Type);
            Assert.Equal(mockTransaction.Amount, transaction.Amount);
           
            mockDbContext.Verify(); // Verify that SaveChangesAsync was called
        }

        [Fact]
        public void PrintStatement_InvalidInput_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            string input = "";

            // Act
            var result = bankingService.PrintStatement(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid input format. Please try again.", result.Message);
        }

        [Fact]
        public void PrintStatement_AccountNotFound_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var accountNumber = "ACC001";
            var month = "07";
            string input = $"{accountNumber}|{month}";

            // Mock the necessary data in the DbContext for the account
            var mockAccount = new Account(); // Empty  account
          
            mockDbContext.Setup(m => m.Accounts).Returns(MockDbSet<Account>(mockAccount));
            mockDbContext.Setup(m => m.SaveChangesAsync(new CancellationToken())).Verifiable();

            // Act
            var result = bankingService.PrintStatement(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Account not found.", result.Message);
        }
        [Fact]
        public void PrintStatement_Removes_DuplicateInterestTransaction_PerMonth()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            
            var accountNumber = "12345";
            var month = "05";
            string input = $"{accountNumber}|{month}";

            var effectiveData = "20230503";
            var ruleId = "RUL001";
            var rate = 3;
            var mockRule = new InterestRule { Date = effectiveData, RuleId = ruleId, Rate = rate };
            mockDbContext.Setup(m => m.InterestRules).Returns(MockDbSet<InterestRule>(mockRule));

            // Create a mock account with transactions
            var transactions = new List<Transaction>
        {
            new Transaction { Date = "20230501", Account = accountNumber, Type = TransactionTypes.D, Amount = 100 },
            new Transaction { Date = "20230502", Account = accountNumber, Type = TransactionTypes.W, Amount = 50 },
            new Transaction { Date = "20230531", Account = accountNumber, Type = TransactionTypes.I, Amount = 10 } // Interest transaction to be removed
        };
            var account = new Account { AccountNumber = accountNumber, Transactions = transactions };
            mockDbContext.Setup(db => db.Accounts).Returns(MockDbSet<Account>(account));
            mockDbContext.Setup(m => m.SaveChangesAsync(new CancellationToken())).Verifiable();

            // Act
            var result = bankingService.PrintStatement(input);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(3, result.Values?.Count);
            Assert.DoesNotContain(result.Values, txn => txn.Type == TransactionTypes.I && txn.Date == "20230531" && txn.Amount==10);
        }

        [Fact]
        public void DefineInterestRules_ValidInput_ReturnsValidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var effectiveData = "20230703";
            var ruleId = "RUL001";
            var rate = 3;
            string input = $"{effectiveData}|{ruleId}|{rate}";
            var mockRule = new InterestRule { Date = effectiveData, RuleId = ruleId, Rate = rate };
            mockDbContext.Setup(m => m.InterestRules).Returns(MockDbSet<InterestRule>(mockRule));

            mockDbContext.Setup(m => m.SaveChangesAsync(new CancellationToken())).Verifiable();

            // Act
            var result = bankingService.DefineInterestRules(input);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.Message);
            Assert.NotNull(result.Values);
            Assert.Single(result.Values);

            var interestRule = result.Values[0];
            Assert.Equal(ruleId, interestRule.RuleId);
            Assert.Equal(effectiveData, interestRule.Date);
            Assert.Equal(rate, interestRule.Rate);

            mockDbContext.Verify(); // Verify that SaveChangesAsync was called
        }

        [Fact]
        public void DefineInterestRules_InvalidInput_ReturnsInvalidOutput()
        {
            // Arrange
            var dbContextMock = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(dbContextMock.Object);
            string input = "";

            // Act
            var result = bankingService.DefineInterestRules(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid input format. Please try again.", result.Message);
        }
        [Fact]
        public void DefineInterestRules_InValidRateInput_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var effectiveData = "20230703";
            var ruleId = "RUL001";
            var rate = 103;
            string input = $"{effectiveData}|{ruleId}|{rate}";

            // Act
            var result = bankingService.DefineInterestRules(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid rate. Please enter a rate between 0 and 100.", result.Message);

        }

        [Fact]
        public void DefineInterestRules_InValidDateInput_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var effectiveData = "20231303";
            var ruleId = "RUL001";
            var rate = 3;
            string input = $"{effectiveData}|{ruleId}|{rate}";

            // Act
            var result = bankingService.DefineInterestRules(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid date entered.", result.Message);

        }

        [Fact]
        public void InputTransactions_ValidInput_ReturnsValidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var date = "20230703";
            var accountNumber = "ACC001";
            var amount = 1000;
            var trnType = TransactionTypes.D;
            string input = $"{date}|{accountNumber}|{trnType}|{amount}";

            // Mock the necessary data in the DbContext for the account and transactions
            var mockAccount = new Account { AccountNumber = accountNumber };
            var mockTransaction = new Transaction
            {
                Date = date,
                Account = accountNumber,
                Type = trnType,
                Amount = amount,
                Id = $"{date}-1"
            };
            mockAccount.Transactions.Add(mockTransaction);
            mockDbContext.Setup(m => m.Accounts).Returns(MockDbSet<Account>(mockAccount));


            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.Message);
            Assert.NotNull(result.Values);
            Assert.Equal(2,result.Values.Count);

            var transaction = result.Values[0];
            Assert.Equal(date, transaction.Date);
            Assert.Equal(accountNumber, transaction.Account);
            Assert.Equal(trnType, transaction.Type);
            Assert.Equal(amount, transaction.Amount);

            mockDbContext.Verify(); // Verify that SaveChangesAsync was called
        }
        [Fact]
        public void InputTransactions_InValidAmountInput_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var date = "20230703";
            var accountNumber = "ACC001";
            var amount = -1000;
            var trnType = TransactionTypes.D;
            string input = $"{date}|{accountNumber}|{trnType}|{amount}";

            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid amount. Please enter a positive number.", result.Message);

        }

        [Fact]
        public void InputTransactions_InValidDateInput_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var date = "20231303";
            var accountNumber = "ACC001";
            var amount = 1000;
            var trnType = TransactionTypes.D;
            string input = $"{date}|{accountNumber}|{trnType}|{amount}";

            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid date entered.", result.Message);

        }
        [Fact]
        public void InputTransactions_InValidInput_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
        
            string input = "";

            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Invalid input format. Please try again.", result.Message);

        }

        [Fact]
        public void InputTransactions_NewInput_CreateAccount_ReturnsValidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var date = "20230703";
            var accountNumber = "ACC001";
            var amount = 1000;
            var trnType = TransactionTypes.D;
            string input = $"{date}|{accountNumber}|{trnType}|{amount}";

            // Mock the necessary data in the DbContext for the account and transactions
            var mockAccount = new Account(); // Set Empty Account
            mockDbContext.Setup(m => m.Accounts).Returns(MockDbSet<Account>(mockAccount));


            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.Message);
            Assert.NotNull(result.Values);
            Assert.Equal(1, result.Values.Count);

            var transaction = result.Values[0];
            Assert.Equal(date, transaction.Date);
            Assert.Equal(accountNumber, transaction.Account);
            Assert.Equal(trnType, transaction.Type);
            Assert.Equal(amount, transaction.Amount);

            mockDbContext.Verify(); // Verify that SaveChangesAsync was called
        }
        [Fact]
        public void InputTransactions_NewAccount_CannotBeWithdrawal_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var date = "20230703";
            var accountNumber = "ACC001";
            var amount = 1000;
            var trnType = TransactionTypes.W;
            string input = $"{date}|{accountNumber}|{trnType}|{amount}";

            // Mock the necessary data in the DbContext for the account and transactions
            var mockAccount = new Account { AccountNumber = accountNumber };
            
            mockDbContext.Setup(m => m.Accounts).Returns(MockDbSet<Account>(mockAccount));

            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("First transaction on an account cannot be a withdrawal.", result.Message);
        }
        [Fact]
        public void InputTransactions_InsufficientBalance_ReturnsInvalidOutput()
        {
            // Arrange
            var mockDbContext = new Mock<IMyBankDbContext>();
            var bankingService = new BankingService(mockDbContext.Object);
            var date = "20230703";
            var accountNumber = "ACC001";
            var amount = 1000;
            var trnType = TransactionTypes.W;
            string input = $"{date}|{accountNumber}|{trnType}|{amount}";

            // Mock the necessary data in the DbContext for the account and transactions
            var mockAccount = new Account { AccountNumber = accountNumber };
            var mockTransaction = new Transaction
            {
                Date = date,
                Account = accountNumber,
                Type = trnType,
                Amount = amount,
                Id = $"{date}-1"
            };
            mockAccount.Transactions.Add(mockTransaction);
            mockDbContext.Setup(m => m.Accounts).Returns(MockDbSet<Account>(mockAccount));
           

            // Act
            var result = bankingService.InputTransactions(input);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.Values);
            Assert.Equal("Insufficient balance for withdrawal.", result.Message);
        }

        // Helper method to create a mocked DbSet<T>
        public static DbSet<T> MockDbSet<T>(params T[] entities) where T : class
        {
            var queryable = entities.AsQueryable();

            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return mockSet.Object;
        }
      
    }
}