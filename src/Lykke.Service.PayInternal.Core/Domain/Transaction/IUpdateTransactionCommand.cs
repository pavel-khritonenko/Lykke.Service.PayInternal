﻿using System;

namespace Lykke.Service.PayInternal.Core.Domain.Transaction
{
    public interface IUpdateTransactionCommand
    {
        string TransactionId { get; set; }

        BlockchainType Blockchain { get; set; }

        string WalletAddress { get; set; }

        double Amount { get; set; }

        int Confirmations { get; set; }

        string BlockId { get; set; }

        DateTime? FirstSeen { get; set; }

        bool IsPayment();
    }
}
