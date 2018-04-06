﻿using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.PayInternal.Contract;
using Lykke.Service.PayInternal.Core.Domain.Transaction;
using Lykke.Service.PayInternal.Core.Services;
using Lykke.Service.PayInternal.Core.Settings.ServiceSettings;

namespace Lykke.Service.PayInternal.Rabbit.Publishers
{
    [UsedImplicitly]
    public class TransactionPublisher : ITransactionPublisher, IStartable, IStopable
    {
        private readonly ILog _log;
        private readonly RabbitMqSettings _settings;
        private RabbitMqPublisher<NewTransactionMessage> _publisher;

        public TransactionPublisher(ILog log, RabbitMqSettings settings)
        {
            _log = log;
            _settings = settings;
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public async Task PublishAsync(IPaymentRequestTransaction transaction)
        {
            await _publisher.ProduceAsync(new NewTransactionMessage
            {
                Id = transaction.Id,
                AssetId = transaction.AssetId,
                Amount = transaction.Amount,
                Confirmations = transaction.Confirmations,
                BlockId = transaction.BlockId,
                Blockchain = Enum.Parse<BlockchainType>(transaction.Blockchain.ToString()),
                DueDate = transaction.DueDate
            });
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForPublisher(_settings.ConnectionString, _settings.TransactionUpdatesExchangeName);

            settings.MakeDurable();

            _publisher = new RabbitMqPublisher<NewTransactionMessage>(settings)
                .SetSerializer(new JsonMessageSerializer<NewTransactionMessage>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .PublishSynchronously()
                .SetLogger(_log)
                .Start();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }
    }
}
