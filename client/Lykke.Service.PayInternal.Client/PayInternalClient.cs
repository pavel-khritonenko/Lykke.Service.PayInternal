﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.PayInternal.Client.Api;
using Lykke.Service.PayInternal.Client.Exceptions;
using Lykke.Service.PayInternal.Client.Models.Asset;
using Lykke.Service.PayInternal.Client.Models.Merchant;
using Lykke.Service.PayInternal.Client.Models.Order;
using Lykke.Service.PayInternal.Client.Models.PaymentRequest;
using Lykke.Service.PayInternal.Client.Models.Transactions;
using Lykke.Service.PayInternal.Client.Models.Wallets;
using Microsoft.Extensions.PlatformAbstractions;
using Refit;

namespace Lykke.Service.PayInternal.Client
{
    public class PayInternalClient : IPayInternalClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IPayInternalApi _payInternalApi;
        private readonly IMerchantsApi _merchantsApi;
        private readonly IOrdersApi _ordersApi;
        private readonly IPaymentRequestsApi _paymentRequestsApi;
        private readonly IAssetsApi _assetsApi;
        private readonly ApiRunner _runner;

        public PayInternalClient(PayInternalServiceClientSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrEmpty(settings.ServiceUrl))
                throw new Exception("Service URL required");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(settings.ServiceUrl),
                DefaultRequestHeaders =
                {
                    {
                        "User-Agent",
                        $"{PlatformServices.Default.Application.ApplicationName}/{PlatformServices.Default.Application.ApplicationVersion}"
                    }
                }
            };

            _payInternalApi = RestService.For<IPayInternalApi>(_httpClient);
            _merchantsApi = RestService.For<IMerchantsApi>(_httpClient);
            _ordersApi = RestService.For<IOrdersApi>(_httpClient);
            _paymentRequestsApi = RestService.For<IPaymentRequestsApi>(_httpClient);
            _assetsApi = RestService.For<IAssetsApi>(_httpClient);
            _runner = new ApiRunner();
        }

        public async Task<WalletAddressResponse> CreateAddressAsync(CreateWalletRequest request)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.CreateAddressAsync(request));
        }

        public async Task<IEnumerable<WalletStateResponse>> GetNotExpiredWalletsAsync()
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.GetNotExpiredWalletsAsync());
        }

        public async Task CreatePaymentTransactionAsync(CreateTransactionRequest request)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.CreatePaymentTransactionAsync(request));
        }

        public async Task UpdateTransactionAsync(UpdateTransactionRequest request)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.UpdateTransactionAsync(request));
        }

        public async Task<IReadOnlyList<MerchantModel>> GetMerchantsAsync()
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.GetAllAsync());
        }
        
        public async Task<MerchantModel> GetMerchantByIdAsync(string merchantId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.GetByIdAsync(merchantId));
        }

        public async Task<MerchantModel> CreateMerchantAsync(CreateMerchantRequest request)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.CreateAsync(request));
        }

        public async Task UpdateMerchantAsync(UpdateMerchantRequest request)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.UpdateAsync(request));
        }

        public async Task SetMerchantPublicKeyAsync(string merchantId, byte[] content)
        {
            var streamPart = new StreamPart(new MemoryStream(content), "public.key");

            await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.SetPublicKeyAsync(merchantId, streamPart));
        }

        public async Task DeleteMerchantAsync(string merchantId)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.DeleteAsync(merchantId));
        }
        
        public async Task<OrderModel> GetOrderAsync(string merchantId, string paymentRequestId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _ordersApi.GetByIdAsync(merchantId, paymentRequestId));
        }

        public async Task<IReadOnlyList<PaymentRequestModel>> GetPaymentRequestsAsync(string merchantId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.GetAllAsync(merchantId));
        }

        public async Task<PaymentRequestModel> GetPaymentRequestAsync(string merchantId, string paymentRequestId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.GetAsync(merchantId, paymentRequestId));
        }

        public async Task<PaymentRequestDetailsModel> GetPaymentRequestDetailsAsync(string merchantId, string paymentRequestId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.GetDetailsAsync(merchantId, paymentRequestId));
        }

        public async Task<PaymentRequestModel> GetPaymentRequestByAddressAsync(string walletAddress)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.GetByAddressAsync(walletAddress));
        }

        public async Task<PaymentRequestModel> CreatePaymentRequestAsync(CreatePaymentRequestModel model)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.CreateAsync(model));
        }

        public async Task<PaymentRequestDetailsModel> ChechoutAsync(string merchantId, string paymentRequestId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.ChechoutAsync(merchantId, paymentRequestId));
        }

        public async Task<BtcTransferResponse> BtcFreeTransferAsync(BtcFreeTransferRequest request)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.BtcFreeTransferAsync(request));
        }

        public async Task<IEnumerable<TransactionStateResponse>> GetAllMonitoredTransactionsAsync()
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.GetAllMonitoredTransactionsAsync());
        }

        public async Task<RefundResponse> RefundAsync(RefundRequestModel request)
        {
            return await _runner.RunAsync(() => _paymentRequestsApi.RefundAsync(request), ExceptionFactories.CreateRefundException);
        }

        public async Task<AvailableAssetsResponse> ResolveAvailableAssetsAsync(string merchantId, AssetAvailabilityType type)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _merchantsApi.GetAvailableAssetsAsync(merchantId, type));
        }

        public async Task<AvailableAssetsResponse> GetGeneralAvailableAssetsAsync(AssetAvailabilityType type)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _assetsApi.GetGeneralAvailableAssetsAsync(type));
        }

        public async Task<AvailableAssetsByMerchantResponse> GetPersonalAvailableAssetsAsync(string merchantId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _assetsApi.GetPersonalAvailableAssetsAsync(merchantId));
        }

        public async Task SetGeneralAvailableAssetsAsync(UpdateAssetAvailabilityRequest request)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _assetsApi.SetGeneralAvailableAssetsAsync(request));
        }

        public async Task SetPersonalAvailableAssetsAsync(UpdateAssetAvailabilityByMerchantRequest request)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _assetsApi.SetPersonalAvailableAssetsAsync(request));
        }

        public async Task CancelAsync(string merchantId, string paymentRequestId)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _paymentRequestsApi.CancelAsync(merchantId, paymentRequestId));
        }

        public async Task SetTransactionExpiredAsync(TransactionExpiredRequest request)
        {
            await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.SetTransactionExpiredAsync(request));
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task<IReadOnlyList<TransactionByPaymentRequestResponse>> GetTransactionsByPaymentRequestAsync(string paymentRequestId)
        {
            return await _runner.RunWithDefaultErrorHandlingAsync(() => _payInternalApi.GetTransactionsByPaymentRequestAsync(paymentRequestId));
        }
    }
}
