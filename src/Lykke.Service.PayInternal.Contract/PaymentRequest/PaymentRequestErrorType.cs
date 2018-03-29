﻿namespace Lykke.Service.PayInternal.Contract.PaymentRequest
{
    /// <summary>
    /// Payment request error types
    /// </summary>
    public enum PaymentRequestErrorType
    {
        /// <summary>
        /// Default value, no errors
        /// </summary>
        None = 0,

        /// <summary>
        /// Amount paid is more than required
        /// </summary>
        PaymentAmountAbove,

        /// <summary>
        /// Amount paid is less than required
        /// </summary>
        PaymentAmountBelow,

        /// <summary>
        /// Payment order expired
        /// </summary>
        PaymentExpired,

        /// <summary>
        /// Refund was not confirmed before expiration date
        /// </summary>
        RefundNotConfirmed
    }
}
