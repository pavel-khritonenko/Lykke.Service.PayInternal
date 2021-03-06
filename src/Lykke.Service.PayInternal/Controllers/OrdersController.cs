﻿using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.PayInternal.Core.Domain.Order;
using Lykke.Service.PayInternal.Core.Domain.PaymentRequests;
using Lykke.Service.PayInternal.Core.Exceptions;
using Lykke.Service.PayInternal.Core.Services;
using Lykke.Service.PayInternal.Filters;
using Lykke.Service.PayInternal.Models.Orders;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.PayInternal.Controllers
{
    [Route("api")]
    public class OrdersController : Controller
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IOrderService _orderService;
        private readonly ILog _log;

        public OrdersController(
            IPaymentRequestService paymentRequestService,
            IOrderService orderService,
            ILog log)
        {
            _paymentRequestService = paymentRequestService;
            _orderService = orderService;
            _log = log;
        }

        /// <summary>
        /// Returns an order by id.
        /// </summary>
        /// <param name="paymentRequestId">The payment request id.</param>
        /// <param name="orderId">The order id.</param>
        /// <returns>The payment request order.</returns>
        /// <response code="200">The payment request order.</response>
        [HttpGet]
        [Route("merchants/paymentrequests/{paymentRequestId}/orders/{orderId}")]
        [SwaggerOperation("OrdersGetByPaymentRequestId")]
        [ProducesResponseType(typeof(OrderModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAsync(string paymentRequestId, string orderId)
        {
            try
            {
                IOrder order = await _orderService.GetAsync(paymentRequestId, orderId);

                if (order == null)
                    return NotFound();

                var model = Mapper.Map<OrderModel>(order);

                return Ok(model);
            }
            catch (Exception exception)
            {
                await _log.WriteErrorAsync(nameof(OrdersController), nameof(GetAsync),
                    new {PaymentRequestId = paymentRequestId}.ToJson(), exception);

                throw;
            }
        }

        /// <summary>
        /// Creates an order if it does not exist or expired.
        /// </summary>
        /// <param name="model">The order creation information.</param>
        /// <returns>An active order related with payment request.</returns>
        /// <response code="200">An active order related with payment request.</response>
        [HttpPost]
        [Route("orders")]
        [SwaggerOperation("OrdersChechout")]
        [ProducesResponseType(typeof(OrderModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> ChechoutAsync([FromBody] ChechoutRequestModel model)
        {
            try
            {
                IPaymentRequest paymentRequest =
                    await _paymentRequestService.CheckoutAsync(model.MerchantId, model.PaymentRequestId, model.Force);

                IOrder order = await _orderService.GetAsync(paymentRequest.Id, paymentRequest.OrderId);

                return Ok(Mapper.Map<OrderModel>(order));
            }
            catch (AssetUnknownException assetEx)
            {
                await _log.WriteErrorAsync(nameof(OrdersController), nameof(ChechoutAsync),
                    new {assetEx.Asset}.ToJson(), assetEx);

                return BadRequest(ErrorResponse.Create(assetEx.Message));
            }
            catch (MarkupNotFoundException markupEx)
            {
                await _log.WriteErrorAsync(nameof(OrdersController), nameof(ChechoutAsync),
                    new { markupEx.MerchantId, markupEx.AssetPairId }.ToJson(), markupEx);

                return BadRequest(ErrorResponse.Create(markupEx.Message));
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersController), nameof(ChechoutAsync),
                    model.ToJson(), ex);

                throw;
            }
        }
    }
}
