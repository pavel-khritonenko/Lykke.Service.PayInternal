﻿using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.MarketProfile.Client;
using Lykke.Service.MarketProfile.Client.Models;
using Lykke.Service.PayInternal.Core;
using Lykke.Service.PayInternal.Core.Domain;
using Lykke.Service.PayInternal.Core.Domain.Markup;
using Lykke.Service.PayInternal.Core.Exceptions;
using Lykke.Service.PayInternal.Core.Services;
using Lykke.Service.PayInternal.Core.Settings.ServiceSettings;

namespace Lykke.Service.PayInternal.Services
{
    public class CalculationService : ICalculationService
    {
        private readonly ILykkeMarketProfile _marketProfileServiceClient;
        private readonly IAssetsLocalCache _assetsLocalCache;
        private readonly LpMarkupSettings _lpMarkupSettings;
        private readonly ILog _log;

        public CalculationService(
            ILykkeMarketProfile marketProfileServiceClient,
            IAssetsLocalCache assetsLocalCache,
            LpMarkupSettings lpMarkupSettings,
            ILog log)
        {
            _marketProfileServiceClient = marketProfileServiceClient ??
                                          throw new ArgumentNullException(nameof(marketProfileServiceClient));
            _assetsLocalCache = assetsLocalCache ?? throw new ArgumentNullException(nameof(assetsLocalCache));
            _lpMarkupSettings = lpMarkupSettings ?? throw new ArgumentNullException(nameof(lpMarkupSettings));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<decimal> GetAmountAsync(string baseAssetId, string quotingAssetId, decimal amount, IRequestMarkup requestMarkup,
            IMarkup merchantMarkup)
        {
            var rate = await GetRateAsync(baseAssetId, quotingAssetId, requestMarkup.Percent, requestMarkup.Pips, merchantMarkup);

            await _log.WriteInfoAsync(nameof(CalculationService), nameof(GetAmountAsync), new
            {
                baseAssetId,
                quotingAssetId,
                amount,
                requestMarkup,
                merchantMarkup,
                rate
            }.ToJson(), "Rate calculation");

            decimal result = (amount + (decimal) requestMarkup.FixedFee + merchantMarkup.FixedFee) / rate;

            Asset baseAsset = await _assetsLocalCache.GetAssetByIdAsync(baseAssetId);

            decimal roundedResult = decimal.Round(result, baseAsset.Accuracy, MidpointRounding.AwayFromZero);

            return roundedResult;
        }

        public async Task<decimal> GetRateAsync(
            string baseAssetId, 
            string quotingAssetId,
            double markupPercent,
            int markupPips,
            IMarkup merchantMarkup)
        {
            double askPrice, bidPrice;

            AssetPair priceAssetPair = null, assetPair = null;

            if (!string.IsNullOrEmpty(merchantMarkup.PriceAssetPairId))
            {
                await _log.WriteInfoAsync(nameof(CalculationService), nameof(GetRateAsync),
                    new {merchantMarkup.PriceAssetPairId}.ToJson(), "Price asset pair will be used");

                priceAssetPair = await _assetsLocalCache.GetAssetPairByIdAsync(merchantMarkup.PriceAssetPairId);

                AssetPairModel assetPairRate = await InvokeMarketProfileServiceAsync(priceAssetPair.Id);

                await _log.WriteInfoAsync(nameof(CalculationService), nameof(GetRateAsync),
                    new {PriceMethod = merchantMarkup.PriceMethod.ToString()}.ToJson(), "Price method");

                switch (merchantMarkup.PriceMethod)
                {
                    case PriceMethod.None:
                    case PriceMethod.Direct:
                        askPrice = assetPairRate.AskPrice;
                        bidPrice = assetPairRate.BidPrice;
                        break;
                    case PriceMethod.Reverse:
                        askPrice = Math.Abs(assetPairRate.AskPrice) > 0
                            ? 1 / assetPairRate.AskPrice
                            : throw new MarketPriceZeroException("ask");
                        bidPrice = Math.Abs(assetPairRate.BidPrice) > 0
                            ? 1 / assetPairRate.BidPrice
                            : throw new MarketPriceZeroException("bid");
                        break;
                    default:
                        throw new UnexpectedAssetPairPriceMethodException(merchantMarkup.PriceMethod);
                }
            } 
            else 
            {
                assetPair = await _assetsLocalCache.GetAssetPairAsync(baseAssetId, quotingAssetId);

                if (assetPair != null)
                {
                    await _log.WriteInfoAsync(nameof(CalculationService), nameof(GetRateAsync),
                        new {AssetPairId = assetPair.Id}.ToJson(), "Asset pair will be used");

                    AssetPairModel assetPairRate = await InvokeMarketProfileServiceAsync(assetPair.Id);

                    askPrice = assetPairRate.AskPrice;

                    bidPrice = assetPairRate.BidPrice;
                }
                else
                {
                    askPrice = bidPrice = 1D;
                }
            }

            await _log.WriteInfoAsync(nameof(CalculationService), nameof(GetRateAsync),
                new {askPrice, bidPrice}.ToJson(), "Market rate that will be used for calculation");

            Asset baseAsset = await _assetsLocalCache.GetAssetByIdAsync(baseAssetId);

            int pairAccuracy = priceAssetPair?.Accuracy ?? assetPair?.Accuracy ?? baseAsset.Accuracy;

            return CalculatePrice(askPrice, bidPrice, pairAccuracy, baseAsset.Accuracy, markupPercent, markupPips,
                PriceCalculationMethod.ByBid, merchantMarkup);
        }

        public async Task<AmountFullFillmentStatus> CalculateBtcAmountFullfillmentAsync(decimal plan, decimal fact)
        {
            if (plan < 0)
                throw new NegativeValueException(plan);

            if (fact < 0)
                throw new NegativeValueException(fact);

            var asset = await _assetsLocalCache.GetAssetByIdAsync(LykkeConstants.BitcoinAsset);

            decimal diff = plan - fact;

            bool fullfilled = Math.Abs(diff) < asset.Accuracy.GetMinValue();

            if (fullfilled)
                return AmountFullFillmentStatus.Exact;

            return diff > 0 ? AmountFullFillmentStatus.Below : AmountFullFillmentStatus.Above;
        }

        public decimal CalculatePrice(
            double askPrice, 
            double bidPrice,
            int pairAccuracy,
            int assetAccuracy,
            double markupPercent,
            int markupPips,
            PriceCalculationMethod priceValueType,
            IMarkup merchantMarkup)
        {
            _log.WriteInfoAsync(nameof(CalculationService), nameof(CalculatePrice), new {askPrice, bidPrice}.ToJson(),
                "Rate calculation").GetAwaiter().GetResult();

            double originalPrice =
                GetOriginalPriceByMethod(bidPrice, askPrice, priceValueType);

            double spread = GetSpread(originalPrice, merchantMarkup.DeltaSpread);

            double priceWithSpread = GetPriceWithSpread(originalPrice, spread, priceValueType);

            double lpFee = GetMerchantFee(priceWithSpread, merchantMarkup.Percent);

            double lpPips = GetMerchantPips(merchantMarkup.Pips);

            double fee = GetMarkupFeePerRequest(priceWithSpread, markupPercent);

            decimal delta = GetDelta(spread, lpFee, fee, lpPips, markupPips, pairAccuracy);

            decimal result = GetPriceWithDelta(originalPrice, delta, priceValueType);

            return GetRoundedPrice(result, pairAccuracy, assetAccuracy, priceValueType);
        }

        public double GetOriginalPriceByMethod(double bid, double ask, PriceCalculationMethod method)
        {
            switch (method)
            {
                case PriceCalculationMethod.ByAsk: return ask;
                case PriceCalculationMethod.ByBid: return bid;
                default: throw new UnexpectedPriceCalculationMethodException(method);
            }
        }

        public double GetSpread(double originalPrice, decimal deltaSpreadPercent)
        {
            if (deltaSpreadPercent < 0)
                throw new NegativeValueException(deltaSpreadPercent);

            return originalPrice * (double) deltaSpreadPercent / 100;
        }

        public double GetPriceWithSpread(double originalPrice, double spread, PriceCalculationMethod method)
        {
            switch (method)
            {
                case PriceCalculationMethod.ByBid: return originalPrice - spread;
                case PriceCalculationMethod.ByAsk: return originalPrice + spread;
                default: throw new UnexpectedPriceCalculationMethodException(method);
            }
        }

        public double GetMerchantFee(double originalPrice, decimal merchantPercent)
        {
            var percent = merchantPercent < 0 ? _lpMarkupSettings.Percent : (double) merchantPercent;

            return originalPrice * percent / 100;
        }

        public double GetMerchantPips(double merchantPips)
        {
            return merchantPips < 0 ? _lpMarkupSettings.Pips : merchantPips;
        }

        public double GetMarkupFeePerRequest(double originalPrice, double markupPercentPerPerquest)
        {
            if (markupPercentPerPerquest < 0)
                throw new NegativeValueException((decimal) markupPercentPerPerquest);

            return originalPrice * markupPercentPerPerquest / 100;
        }

        public decimal GetDelta(
            double spread,
            double lpFee,
            double markupFee,
            double lpPips,
            double markupPips,
            int accuracy)
        {
            double totalFee = lpFee + markupFee;

            double totalPips = lpPips + markupPips;

            return
                (decimal) spread +
                (decimal) totalFee +
                (decimal) totalPips * accuracy.GetMinValue();
        }

        public decimal GetPriceWithDelta(double originalPrice, decimal delta, PriceCalculationMethod method)
        {
            switch (method)
            {
                case PriceCalculationMethod.ByBid: return (decimal) originalPrice - delta;
                case PriceCalculationMethod.ByAsk: return (decimal) originalPrice + delta;
                default: throw new UnexpectedPriceCalculationMethodException(method);
            }
        }

        public decimal GetRoundedPrice(decimal originalPrice, int pairAccuracy, int assetAccuracy,
            PriceCalculationMethod method)
        {
            decimal result;

            switch (method)
            {
                case PriceCalculationMethod.ByBid:
                    result = originalPrice - pairAccuracy.GetMinValue() * (decimal) 0.5;
                    break;
                case PriceCalculationMethod.ByAsk:
                    result = originalPrice + pairAccuracy.GetMinValue() * (decimal) 0.49;
                    break;
                default: throw new UnexpectedPriceCalculationMethodException(method);
            }

            decimal rounded = Math.Round(result, assetAccuracy);

            int mult = (int) Math.Pow(10, assetAccuracy);

            decimal ceiled = Math.Ceiling(rounded * mult) / mult;

            return ceiled < 0 ? 0 : ceiled;
        }

        private async Task<AssetPairModel> InvokeMarketProfileServiceAsync(string assetPairId)
        {
            object response = await _marketProfileServiceClient.ApiMarketProfileByPairCodeGetAsync(assetPairId);

            if (response is ErrorModel error)
            {
                throw new Exception(error.Message);
            }

            if (response is AssetPairModel assetPairRate)
            {
                return assetPairRate;
            }
            
            throw new Exception("Unknown MarketProfile API response");
        }
    }
}
