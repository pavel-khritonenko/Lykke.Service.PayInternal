﻿using Lykke.Service.PayInternal.Core.Domain.Wallet;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.PayInternal.AzureRepositories.Wallet
{
    public class WalletEntity : TableEntity, IWalletEntity
    {
        public static class ByMerchant
        {
            public static string GeneratePartitionKey(string merchantId)
            {
                return merchantId;
            }

            public static string GenerateRowKey(string address)
            {
                return address;
            }

            public static WalletEntity Create(IWalletEntity src)
            {
                return new WalletEntity
                {
                    PartitionKey = GeneratePartitionKey(src.MerchantId),
                    RowKey = GenerateRowKey(src.Address),
                    MerchantId = src.MerchantId,
                    Address = src.Address,
                    Data = src.Data
                };
            }
        }

        public string MerchantId { get; set; }
        public string Address { get; set; }
        public string Data { get; set; }
    }
}
