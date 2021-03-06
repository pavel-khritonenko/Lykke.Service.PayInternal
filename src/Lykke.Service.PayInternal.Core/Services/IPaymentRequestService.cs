﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PayInternal.Core.Domain.PaymentRequests;

namespace Lykke.Service.PayInternal.Core.Services
{
    public interface IPaymentRequestService
    {
        Task<IReadOnlyList<IPaymentRequest>> GetAsync(string merchantId);
        
        Task<IPaymentRequest> GetAsync(string merchantId, string paymentRequestId);

        Task<PaymentRequestRefund> GetRefundInfoAsync(string walletAddress);

        Task<IPaymentRequest> FindAsync(string walletAddress);

        Task<IPaymentRequest> CreateAsync(IPaymentRequest paymentRequest);

        Task CancelAsync(string merchantId, string paymentRequestId);
        
        Task<IPaymentRequest> CheckoutAsync(string merchantId, string paymentRequestId, bool force);

        Task UpdateStatusAsync(string walletAddress, PaymentRequestStatusInfo statusInfo = null);

        Task HandleExpiredAsync();
    }
}
