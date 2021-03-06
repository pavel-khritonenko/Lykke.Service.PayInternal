﻿using System;

namespace Lykke.Service.PayInternal.Core.Domain.Transaction
{
    public interface IUpdateTransactionRequest : IWalletAddressHolder
    {
        string Hash { get; set; }

        double Amount { get; set; }

        int Confirmations { get; set; }

        string BlockId { get; set; }

        DateTime? FirstSeen { get; set; }

        TransactionIdentityType IdentityType { get; set; }

        string Identity { get; set; }
    }
}
