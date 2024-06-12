using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Data.Interfaces;
using ClearBank.DeveloperTest.Services.Interfaces;
using ClearBank.DeveloperTest.Types;
using System.Configuration;

namespace ClearBank.DeveloperTest.Services
{
    public class PaymentService : IPaymentService
    {
        readonly IAccountDataStore _accountDataStore;
        readonly IBackupAccountDataStore _backupAccountDataStore;
        readonly string _dataStoreType;
        public PaymentService(IAccountDataStore accountDataStore, IBackupAccountDataStore backupAccountDataStore) {
            _accountDataStore = accountDataStore;
            _backupAccountDataStore = backupAccountDataStore;
            _dataStoreType  = ConfigurationManager.AppSettings["DataStoreType"];
        } 

        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            var account = GetAccount(request.DebtorAccountNumber);
            var result = new MakePaymentResult();
            ProcessPayment(request, account, result);
            if (result.Success)
            {
                ProcessAccountDeduction(request, account);
                UpdateAccount(_dataStoreType, account);
            }
            return result;
        }
        private Account GetAccount(string accountNumber)
        {
            return _dataStoreType == AccountTypes.BackUp.ToString()
                ? _backupAccountDataStore.GetAccount(accountNumber)
                : _accountDataStore.GetAccount(accountNumber);
        }

        private static void ProcessPayment(MakePaymentRequest request, Account account, MakePaymentResult result)
        {
            if (account == null)
            {
                result.Success = false;
            }
            else
            {
                switch (request.PaymentScheme)
                {
                    case PaymentScheme.Bacs:
                        ProcessBacsPayment(account, result);
                        break;

                    case PaymentScheme.FasterPayments:
                        ProcessFasterPayments(request, account, result);
                        break;

                    case PaymentScheme.Chaps:
                        ProcessChapsPayment(account, result);
                        break;
                }
            }
        }
        private static void ProcessChapsPayment(Account account, MakePaymentResult result)
        {
            if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Chaps))
            {
                result.Success = false;
            }
            else if (account.Status != AccountStatus.Live)
            {
                result.Success = false;
            }
        }
        private static void ProcessFasterPayments(MakePaymentRequest request, Account account, MakePaymentResult result)
        {
            if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.FasterPayments))
            {
                result.Success = false;
            }
            else if (account.Balance < request.Amount)
            {
                result.Success = false;
            }
        }
        private static void ProcessBacsPayment(Account account, MakePaymentResult result)
        {
            if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Bacs))
            {
                result.Success = false;
            }
        }
        private static void ProcessAccountDeduction(MakePaymentRequest request, Account account)
        {
            account.Balance -= request.Amount;
        }
        private void UpdateAccount(string dataStoreType, Account account)
        {
            if (dataStoreType == AccountTypes.BackUp.ToString())
            {
                _backupAccountDataStore.UpdateAccount(account);
            }
            else
            {
                _accountDataStore.UpdateAccount(account);
            }
        }
    }
}
