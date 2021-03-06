﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PayInternal.Core.Domain.Transaction;
using Lykke.Service.PayInternal.Core.Services;

namespace Lykke.Service.PayInternal.Services
{
    public class TransactionsManager : ITransactionsManager
    {
        private readonly ITransactionsService _transactionsService;
        private readonly IPaymentRequestService _paymentRequestService;

        public TransactionsManager(
            ITransactionsService transactionsService,
            IPaymentRequestService paymentRequestService)
        {
            _transactionsService = transactionsService;
            _paymentRequestService = paymentRequestService;
        }

        public async Task<IPaymentRequestTransaction> CreateTransactionAsync(ICreateTransactionCommand command)
        {
            IPaymentRequestTransaction transaction = await _transactionsService.CreateTransactionAsync(command);

            await _paymentRequestService.UpdateStatusAsync(command.WalletAddress);

            return transaction;
        }

        public async Task UpdateTransactionAsync(IUpdateTransactionCommand command)
        {
            await _transactionsService.UpdateAsync(command);

            if (string.IsNullOrEmpty(command.WalletAddress))
            {
                IEnumerable<IPaymentRequestTransaction> txs =
                    await _transactionsService.GetByBcnIdentityAsync(command.Blockchain, command.IdentityType, command.Identity);

                foreach (IPaymentRequestTransaction tx in txs)
                {
                    await _paymentRequestService.UpdateStatusAsync(tx.WalletAddress);
                }
            }
            else
            {
                await _paymentRequestService.UpdateStatusAsync(command.WalletAddress);
            }
        }
    }
}
