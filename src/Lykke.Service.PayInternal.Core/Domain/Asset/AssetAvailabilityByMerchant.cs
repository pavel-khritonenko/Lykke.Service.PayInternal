﻿namespace Lykke.Service.PayInternal.Core.Domain.Asset
{
    public class AssetAvailabilityByMerchant : IAssetAvailabilityByMerchant
    {
        public string MerchantId { get; set; }

        public string PaymentAssets { get; set; }

        public string SettlementAssets { get; set; }
    }
}
