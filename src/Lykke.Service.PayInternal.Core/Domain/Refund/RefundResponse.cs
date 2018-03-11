﻿namespace Lykke.Service.PayInternal.Core.Domain.Refund
{
    public class RefundResponse : IRefund
    {
        public string RefundId { get; set; }
        public string MerchantId { get; set; }
        public string PaymentRequestId { get; set; }
        public string SettlementId { get; set; }
        public decimal Amount { get; set; }
    }
}