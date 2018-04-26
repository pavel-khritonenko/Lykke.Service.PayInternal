﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PayInternal.Core.Domain.Transaction;

namespace Lykke.Service.PayInternal.Core.Services
{
    public interface ITransactionsService
    {
        Task<IReadOnlyList<IPaymentRequestTransaction>> GetByWalletAsync(string walletAddress);

        Task<IPaymentRequestTransaction> GetByIdAsync(BlockchainType blockchain, TransactionIdentityType identityType, string identity);

        Task<IReadOnlyList<IPaymentRequestTransaction>> GetConfirmedAsync(string walletAddress);

        Task<IReadOnlyList<IPaymentRequestTransaction>> GetNotExpiredAsync();

        Task<IPaymentRequestTransaction> CreateTransactionAsync(ICreateTransactionCommand request);

        Task<IPaymentRequestTransaction> UpdateAsync(IUpdateTransactionCommand request);

        Task<IReadOnlyList<IPaymentRequestTransaction>> GetTransactionsByPaymentRequestAsync(string paymentRequestId);
    }
}
