using ClearBank.DeveloperTest.Data.Interfaces;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Xunit;
using Moq;
using System.Configuration;


namespace ClearBank.DeveloperTest.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IAccountDataStore> _mockAccountDataStore;
        private readonly Mock<IBackupAccountDataStore> _mockBackupAccountDataStore;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _mockAccountDataStore = new Mock<IAccountDataStore>();
            _mockBackupAccountDataStore = new Mock<IBackupAccountDataStore>();
            _paymentService = new PaymentService(_mockAccountDataStore.Object, _mockBackupAccountDataStore.Object);
        }

        [Fact]
        public void MakePayment_WhenAccountIsNull_ShouldReturnFailure()
        {
            // Arrange
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.Bacs };
            _mockAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns((Account)null);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void MakePayment_WithBacsPaymentScheme_ShouldReturnFailure_WhenAccountNotAllowedBacs()
        {
            // Arrange
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.Bacs };
            var account = new Account { AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments };
            _mockAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns(account);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void MakePayment_WithFasterPaymentsScheme_ShouldReturnFailure_WhenInsufficientBalance()
        {
            // Arrange
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.FasterPayments, Amount = 100 };
            var account = new Account { AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments, Balance = 50 };
            _mockAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns(account);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void MakePayment_WithChapsPaymentScheme_ShouldReturnFailure_WhenAccountNotLive()
        {
            // Arrange
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.Chaps };
            var account = new Account { AllowedPaymentSchemes = AllowedPaymentSchemes.Chaps, Status = AccountStatus.Disabled };
            _mockAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns(account);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void MakePayment_WithBackupDataStore_ShouldReturnSuccess_WhenValidPayment()
        {
            // Arrange
            ConfigurationManager.AppSettings["DataStoreType"] = "BackUp";
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.Bacs, Amount = 100 };
            var account = new Account { AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs, Balance = 200 };
            _mockBackupAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns(account);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void MakePayment_WithBackupDataStore_ShouldReturnFailure_WhenAccountIsNull()
        {
            // Arrange
            ConfigurationManager.AppSettings["DataStoreType"] = "BackUp";
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.Bacs };
            _mockBackupAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns((Account)null);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void MakePayment_WithBackupDataStore_ShouldReturnFailure_WhenInsufficientBalance()
        {
            // Arrange
            ConfigurationManager.AppSettings["DataStoreType"] = "BackUp";
            var request = new MakePaymentRequest { DebtorAccountNumber = "403445", PaymentScheme = PaymentScheme.FasterPayments, Amount = 100 };
            var account = new Account { AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments, Balance = 50 };
            _mockBackupAccountDataStore.Setup(x => x.GetAccount(It.IsAny<string>())).Returns(account);

            // Act
            var result = _paymentService.MakePayment(request);

            // Assert
            Assert.False(result.Success);
        }
    }
}
