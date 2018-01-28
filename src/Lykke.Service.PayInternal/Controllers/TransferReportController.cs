﻿using System.Net;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.PayInternal.Core.Services;
using Lykke.Service.PayInternal.Extensions;
using Lykke.Service.PayInternal.Models;
using Lykke.Service.PayInternal.Models.PaymentRequests;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.PayInternal.Controllers
{
    [Route("api")]
    public class TransferReportController : Controller
    {
        private readonly ITransferRequestService _transferRequestService;
        public TransferReportController(ITransferRequestService transferRequestService)
        {
            _transferRequestService = transferRequestService;
        }

        /// <summary>
        /// Update transfer status.
        /// </summary>
        /// <param name="model">Transfer model.</param>
        /// <returns>The Transfer Info.</returns>
        /// <response code="200">The Transfer Info.</response>
        /// <response code="400">Invalid model.</response>
        [HttpPost]
        [Route("transfer/updateStatus")]
        [SwaggerOperation("TransferUpdateStatus")]
        [ProducesResponseType(typeof(PaymentRequestModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateTransferStatusAsync([FromBody] UpdateTransferStatusModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponse().AddErrors(ModelState));

            if (string.IsNullOrEmpty(model.TransferId))
                return BadRequest(ErrorResponse.Create("Transfer id doesn't exist"));

            if (string.IsNullOrEmpty(model.TransactionHash))
                return BadRequest(ErrorResponse.Create("Transaction hash doesn't exist"));

            return Ok(await _transferRequestService.UpdateTransferStatusAsync(model));
        }
    }
}
