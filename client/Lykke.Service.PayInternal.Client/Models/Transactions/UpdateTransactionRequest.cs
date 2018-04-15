﻿using System;
using JetBrains.Annotations;

namespace Lykke.Service.PayInternal.Client.Models.Transactions
{
    public class UpdateTransactionRequest
    {
        public string TransactionId { get; set; }
        public string WalletAddress { get; set; }
        public BlockchainType Blockchain { get; set; }
        public decimal Amount { get; set; }
        public int Confirmations { get; set; }
        [CanBeNull] public string BlockId { get; set; }
        public DateTime? FirstSeen { get; set; }
    }
}
