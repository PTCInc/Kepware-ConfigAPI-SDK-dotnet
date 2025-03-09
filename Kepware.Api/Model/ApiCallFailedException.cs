using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Exception thrown when an API call fails.
    /// </summary>
    public sealed class ApiCallFailedException : Exception
    {
        /// <summary>
        /// Gets the response code associated with the failed API call.
        /// </summary>
        public ApiResponseCode ResponseCode { get; }

        /// <summary>
        /// Gets the endpoint that was called when the error occurred.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCallFailedException"/> class.
        /// </summary>
        /// <param name="responseCode">The response code indicating the failure reason.</param>
        /// <param name="endpoint">The API endpoint that was called.</param>
        /// <param name="message">The error message.</param>
        public ApiCallFailedException(ApiResponseCode responseCode, string endpoint, string message)
            : base($"API call to '{endpoint}' failed with status {responseCode}: {message}")
        {
            ResponseCode = responseCode;
            Endpoint = endpoint;
        }
    }
}
