﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.EthereumCore.Client.Models;
using Lykke.Service.PayInternal.Core;
using Lykke.Service.PayInternal.Core.Domain.Transaction;
using Lykke.Service.PayInternal.Core.Domain.Transfer;
using Lykke.Service.PayInternal.Core.Exceptions;
using Lykke.Service.PayInternal.Core.Services;
using Lykke.Service.PayInternal.Core.Settings.ServiceSettings;

namespace Lykke.Service.PayInternal.Services
{
    public class EthereumApiClient : IBlockchainApiClient
    {
        private readonly IEthereumCoreAPI _ethereumServiceClient;
        private readonly EthereumBlockchainSettings _ethereumSettings;
        private readonly IAssetsLocalCache _assetsLocalCache;
        private readonly ILog _log;

        public EthereumApiClient(
            IEthereumCoreAPI ethereumServiceClient, 
            EthereumBlockchainSettings ethereumSettings, 
            ILog log, 
            IAssetsLocalCache assetsLocalCache)
        {
            _ethereumServiceClient = ethereumServiceClient ?? throw new ArgumentNullException(nameof(ethereumServiceClient));
            _ethereumSettings = ethereumSettings ?? throw new ArgumentNullException(nameof(ethereumSettings));
            _assetsLocalCache = assetsLocalCache ?? throw new ArgumentNullException(nameof(assetsLocalCache));
            _log = log?.CreateComponentScope(nameof(EthereumApiClient)) ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<BlockchainTransferResult> TransferAsync(BlockchainTransferCommand transfer)
        {
            BlockchainTransferResult result = new BlockchainTransferResult { Blockchain = BlockchainType.Ethereum };

            Asset asset = await _assetsLocalCache.GetAssetByIdAsync(transfer.AssetId);

            if (asset.Type != AssetType.Erc20Token || !asset.IsTradable)
                throw new AssetNotSupportedException(asset.Id);

            foreach (TransferAmount transferAmount in transfer.Amounts)
            {
                var transferRequest = Mapper.Map<TransferFromDepositRequest>(transferAmount,
                    opts => opts.Items["TokenAddress"] = asset.AssetAddress);

                object response = await _ethereumServiceClient.ApiLykkePayErc20depositsTransferPostAsync(
                    _ethereumSettings.ApiKey, transferRequest);

                var errorMessage = string.Empty;

                var operationId = string.Empty;

                if (response is ApiException ex)
                {
                    await _log.WriteWarningAsync(nameof(TransferAsync), transferAmount.ToJson(), ex.Error?.ToJson());

                    errorMessage = ex.Error?.Message;
                } else if (response is OperationIdResponse op)
                {
                    operationId = op.OperationId;
                }
                else
                {
                    throw new UnrecognizedApiResponse(response?.GetType().FullName ?? "Response object is null");
                }

                result.Transactions.Add(new BlockchainTransactionResult
                {
                    Amount = transferAmount.Amount,
                    AssetId = asset.Id,
                    Hash = string.Empty,
                    IdentityType = TransactionIdentityType.Specific,
                    Identity = operationId,
                    Sources = new List<string> { transferAmount.Source },
                    Destinations = new List<string> { transferAmount.Destination },
                    Error = errorMessage
                });
            }

            return result;
        }

        public async Task<string> CreateAddressAsync()
        {
            object response = await _ethereumServiceClient.ApiLykkePayErc20depositsPostAsync(_ethereumSettings.ApiKey);

            if (response is ApiException ex)
            {
                await _log.WriteWarningAsync(nameof(CreateAddressAsync), "New erc20 address generation",
                    ex.Error?.Message);

                throw new WalletAddressAllocationException(BlockchainType.Ethereum);
            }

            if (response is RegisterResponse result)
            {
                return result.Contract;
            }

            throw new UnrecognizedApiResponse(response?.GetType().FullName);
        }
    }
}
