﻿using AutoMapper;
using Lykke.Service.EthereumCore.Client.Models;
using Lykke.Service.PayInternal.Core.Domain.PaymentRequests;
using Lykke.Service.PayInternal.Core.Domain.Transaction;
using Lykke.Service.PayInternal.Core.Domain.Transfer;
using Lykke.Service.PayInternal.Services.Domain;
using Lykke.Service.PayInternal.Services.Mapping;

namespace Lykke.Service.PayInternal.Services
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<IPaymentRequestTransaction, PaymentRequestRefundTransaction>(MemberList.Destination)
                .ForMember(dest => dest.Hash, opt => opt.MapFrom(src => src.TransactionId))
                .ForMember(dest => dest.NumberOfConfirmations, opt => opt.MapFrom(src => src.Confirmations))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.CreatedOn));

            CreateMap<IPaymentRequestTransaction, TransferCommand>(MemberList.Destination)
                .ForMember(dest => dest.Amounts, opt => opt.ResolveUsing<RefundAmountResolver>());

            CreateMap<ICreateTransactionRequest, CreateTransactionCommand>(MemberList.Destination)
                .ForMember(dest => dest.DueDate, opt => opt.Ignore())
                .ForMember(dest => dest.TransferId, opt => opt.Ignore())
                .ForMember(dest => dest.Type,
                    opt => opt.ResolveUsing((src, dest, destMember, resContext) =>
                        dest.Type = (TransactionType) resContext.Items["TransactionType"]))
                .ForMember(dest => dest.WalletAddress, opt => opt.ResolveUsing<VirtualAddressResolver>());

            CreateMap<IUpdateTransactionRequest, UpdateTransactionCommand>(MemberList.Destination)
                .ForMember(dest => dest.WalletAddress, opt => opt.ResolveUsing<VirtualAddressResolver>());

            CreateMap<IPaymentRequest, RequestMarkup>(MemberList.Destination)
                .ForMember(dest => dest.Percent, opt => opt.MapFrom(src => src.MarkupPercent))
                .ForMember(dest => dest.Pips, opt => opt.MapFrom(src => src.MarkupPips))
                .ForMember(dest => dest.FixedFee, opt => opt.MapFrom(src => src.MarkupFixedFee));

            CreateMap<TransferAmount, TransferFromDepositRequest>(MemberList.Destination)
                .ForMember(dest => dest.DepositAddress, opt => opt.MapFrom(src => src.Source))
                .ForMember(dest => dest.DestinationAddress, opt => opt.MapFrom(src => src.Destination))
                .ForMember(dest => dest.TokenAddress,
                    opt => opt.ResolveUsing((src, dest, destMemeber, resContext) =>
                        dest.TokenAddress = (string) resContext.Items["TokenAddress"]));

            CreateMap<ICreateTransactionCommand, PaymentRequestTransaction>(MemberList.Source)
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Hash))
                .ForMember(dest => dest.PaymentRequestId,
                    opt => opt.ResolveUsing((src, dest, destMember, resContext) =>
                        dest.PaymentRequestId = ((IPaymentRequest) resContext.Items["PaymentRequest"])?.Id))
                .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.DueDate,
                    opt => opt.ResolveUsing((src, dest, destMember, resContext) =>
                        src.DueDate ?? ((IPaymentRequest) resContext.Items["PaymentRequest"])?.DueDate));

            CreateMap<BlockchainTransactionResult, TransferTransaction>(MemberList.Destination);

            CreateMap<TransferTransaction, TransferTransactionResult>(MemberList.Destination);

            CreateMap<ITransfer, TransferResult>(MemberList.Destination)
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.CreatedOn));

            CreateMap<ICreateTransactionCommand, UpdateTransactionCommand>(MemberList.Destination);
        }
    }
}
