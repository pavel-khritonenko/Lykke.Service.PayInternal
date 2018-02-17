﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PayInternal.Client.Models.PaymentRequest;
using Refit;

namespace Lykke.Service.PayInternal.Client.Api
{
    internal interface IPaymentRequestsApi
    {
        [Get("/api/merchants/{merchantId}/paymentrequests")]
        Task<IReadOnlyList<PaymentRequestModel>> GetAllAsync(string merchantId);
        
        [Get("/api/merchants/{merchantId}/paymentrequests/{paymentRequestId}")]
        Task<PaymentRequestModel> GetAsync(string merchantId, string paymentRequestId);

        [Get("/api/merchants/{merchantId}/paymentrequests/details/{paymentRequestId}")]
        Task<PaymentRequestDetailsModel> GetDetailsAsync(string merchantId, string paymentRequestId);

        [Get("/api/paymentrequests/byAddress/{walletAddress}")]
        Task<PaymentRequestModel> GetByAddressAsync(string walletAddress);
        
        [Post("/api/merchants/paymentrequests")]
        Task<PaymentRequestModel> CreateAsync([Body] CreatePaymentRequestModel model);
        
        [Post("/api/merchants/{merchantId}/paymentrequests/{paymentRequestId}")]
        Task<PaymentRequestDetailsModel> ChechoutAsync(string merchantId, string paymentRequestId);

        [Post("/api/btcTransfer")]
        Task<BtcTransferResponse> BtcFreeTransfer([Body] BtcFreeTransferRequest request);
    }
}
