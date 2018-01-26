﻿using System;

namespace Lykke.Service.PayInternal.Client.Models
{
    public class CreateOrderResponse
    {
        public string OrderId { get; set; }

        public DateTime DueDate { get; set; }

        public string AssetPairId { get; set; }

        public string InvoiceAssetId { get; set; }

        public double InvoiceAmount { get; set; }

        public string ExchangeAssetId { get; set; }

        public double ExchangeAmount { get; set; }

        public string WalletAddress { get; set; }
    }
}