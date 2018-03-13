﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QBitNinja.Client;
using JetBrains.Annotations;
using Lykke.Service.PayInternal.Core.Domain.PaymentRequest;
using Lykke.Service.PayInternal.Core.Domain.Refund;
using Lykke.Service.PayInternal.Core.Domain.Transaction;
using Lykke.Service.PayInternal.Core.Domain.Transfer;
using Lykke.Service.PayInternal.Core.Domain.Wallet;
using Lykke.Service.PayInternal.Core.Exceptions;
using Lykke.Service.PayInternal.Core.Services;
using Lykke.Service.PayInternal.Services.Domain;
// ReSharper disable once RedundantUsingDirective
using NBitcoin;
using QBitNinja.Client.Models;
using TransactionNotFoundException = Lykke.Service.PayInternal.Core.Exceptions.TransactionNotFoundException;

namespace Lykke.Service.PayInternal.Services
{
    [UsedImplicitly]
    public class RefundService : IRefundService
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly QBitNinjaClient _qBitNinjaClient;
        private readonly ITransferService _transferService;
        private readonly ITransactionsService _transactionService;
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IRefundRepository _refundRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly TimeSpan _expirationTime;
        private readonly IBlockchainTransactionRepository _transactionRepository;

        public RefundService(
            QBitNinjaClient qBitNinjaClient,
            ITransferService transferService,
            ITransactionsService transactionService,
            IPaymentRequestService paymentRequestService,
            IRefundRepository refundRepository,
            IWalletRepository walletRepository,
            IBlockchainTransactionRepository transactionRepository,
            TimeSpan expirationTime)
        {
            _qBitNinjaClient =
                qBitNinjaClient ?? throw new ArgumentNullException(nameof(qBitNinjaClient));
            _transferService =
                transferService ?? throw new ArgumentNullException(nameof(transferService));
            _transactionService =
                transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _paymentRequestService =
                paymentRequestService ?? throw new ArgumentNullException(nameof(paymentRequestService));
            _refundRepository =
                refundRepository ?? throw new ArgumentNullException(nameof(refundRepository));
            _walletRepository =
                walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
            _transactionRepository =
                transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _expirationTime = expirationTime;
        }

        public async Task<IRefund> ExecuteAsync(IRefundRequest refund)
        {
            IPaymentRequest paymentRequest = await _paymentRequestService.FindAsync(refund.SourceAddress);

            if (paymentRequest == null)
                throw new PaymentRequestNotFoundException(refund.SourceAddress);

            if (!paymentRequest.MerchantId.Equals(refund.MerchantId))
                throw new PaymentRequestNotFoundException(refund.MerchantId, paymentRequest.Id);

            //todo: in some cases we can't procees if status was PaymentRequestStatus.Error
            if (paymentRequest.Status != PaymentRequestStatus.Confirmed &&
                paymentRequest.Status != PaymentRequestStatus.Error)
                throw new NotAllowedStatusException(paymentRequest.Status.ToString());

            var walletsCheckResult = await _checkupMerchantWallets(refund);

            if (!walletsCheckResult)
                throw new WalletNotFoundException(refund.SourceAddress);

            IEnumerable<IBlockchainTransaction> paymentRequestTxs =
                await _transactionRepository.GetByPaymentRequest(paymentRequest.Id);

            IEnumerable<IBlockchainTransaction> paymentTxs =
                paymentRequestTxs.Where(x => x.TransactionType == TransactionType.Payment).ToList();

            if (!paymentTxs.Any())
                throw new NoTransactionsToRefundException(paymentRequest.Id);

            if (paymentTxs.Count() > 1)
                throw new MultiTransactionRefundNotSupportedException(paymentTxs.Count());

            IBlockchainTransaction txToRefund = paymentTxs.First();

            //todo: if multiple source addresses

            BalanceSummary balanceSummary =
                await _qBitNinjaClient.GetBalanceSummary(BitcoinAddress.Create(refund.SourceAddress));

            decimal spendableSatoshi = balanceSummary.Spendable.Amount.ToDecimal(MoneyUnit.Satoshi);

            //todo: take into account different assets 
            //todo: consider situation if we can make partial refund
            if (spendableSatoshi < txToRefund.Amount)
                throw new NotEnoughMoneyException(spendableSatoshi, txToRefund.Amount);

            if (!string.IsNullOrEmpty(refund.DestinationAddress))
            {
            }
            else
            {
                // transferring money back to wallets where the payment request has been paid from 
            }

            var newRefund = new Refund
            {
                PaymentRequestId = paymentRequest.Id,
                DueDate = DateTime.UtcNow.Add(_expirationTime),
                MerchantId = refund.MerchantId,
                RefundId = Guid.NewGuid().ToString(),
                Amount = txToRefund.Amount
                // TODO: what about settlement ID?
            };

            var newTransfer = new MultipartTransfer
            {
                PaymentRequestId = paymentRequest.Id,
                AssetId = paymentRequest.PaymentAssetId,
                CreationDate = DateTime.UtcNow,
                FeeRate = 0, // TODO: make sure this is correct
                FixedFee = (decimal)paymentRequest.MarkupFixedFee,
                MerchantId = refund.MerchantId,
                TransferId = Guid.NewGuid().ToString(),
                Parts = new List<TransferPart>()
            };

            var result = new RefundResponse
            {
                MerchantId = refund.MerchantId,
                PaymentRequestId = paymentRequest.Id,
                RefundId = newRefund.RefundId,
                DueDate = newRefund.DueDate,
                Amount = txToRefund.Amount
                // TODO: what about settlement ID?
            };

            // The main work below:

            await _refundRepository.AddAsync(newRefund); // Save the refund itself first

            // The simpliest case: we have both source and destionation addresses. Create a new transfer for the whole volume of money from the source.
            if (!string.IsNullOrWhiteSpace(refund.DestinationAddress))
            {
                newTransfer.Parts.Add(
                    new TransferPart
                    {
                        Destination = new AddressAmount
                        {
                            Address = refund.DestinationAddress,
                            Amount = txToRefund.Amount
                        },
                        Sources = new List<AddressAmount>
                        {
                            new AddressAmount
                            {
                                Address = refund.SourceAddress,
                                Amount = txToRefund.Amount
                            }
                        }
                    }
                );

                var executionResult = await _transferService.ExecuteMultipartTransferAsync(newTransfer, TransactionType.Refund); // Execute the transfer for single transaction and check
                if (executionResult.State == TransferExecutionResult.Fail)
                    throw new Exception(executionResult.ErrorMessage);
            }
            // And another case: we have only source, so, we need to reverse all the transactions from the payment request.
            else
            {
                // ATTENTION: currently this code is unreachable due to pre-check of DestinationAddress presense.
                var transactions = await _transactionService.GetConfirmedAsync(paymentRequest.WalletAddress);
                if (transactions == null)
                    throw new TransactionNotFoundException("There are (still) no confirmed transactions for the payment request with the specified wallet address.");

                // ReSharper disable once UnusedVariable
                foreach (var tran in transactions)
                {
                   // TODO: Implement transaction reversing with use of QBitNinja client.
                }
                
                var executionResult = await _transferService.ExecuteMultipartTransferAsync(newTransfer, TransactionType.Refund); // Execute the transfer for multiple transactions and check
                if (executionResult.State != TransferExecutionResult.Success)
                    throw new Exception(executionResult.ErrorMessage);
            }

            // Additionally, process the payment request itself.
            await _paymentRequestService.ProcessAsync(refund.SourceAddress);

            return result;
        }

        public async Task<IRefund> GetStateAsync(string merchantId, string refundId)
        {
            return await _refundRepository.GetAsync(merchantId, refundId);
        }

        private async Task<bool> _checkupMerchantWallets(IRefundRequest refund)
        {
            var wallets = (await _walletRepository.GetByMerchantAsync(refund.MerchantId))?.ToList();

            if (wallets == null || !wallets.Any()) return false;

            // Currently we check up only the source address. But is may be useful to check the destination either.
            return wallets.Any(w => w.Address == refund.SourceAddress);
        }
    }
}
