using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents API response codes according to HTTP standards.
    /// </summary>
    public enum ApiResponseCode
    {
        /// <summary>200 OK - The request was successful.</summary>
        Success = 200,

        /// <summary>201 Created - The request was successful, and a new resource was created.</summary>
        Created = 201,

        /// <summary>202 Accepted - The request has been accepted for processing.</summary>
        Accepted = 202,

        /// <summary>400 Bad Request - The request was invalid or malformed.</summary>
        BadRequest = 400,

        /// <summary>401 Unauthorized - Authentication is required and has failed or not been provided.</summary>
        Unauthorized = 401,

        /// <summary>403 Forbidden - The request was valid, but the server refuses to authorize it.</summary>
        Forbidden = 403,

        /// <summary>404 Not Found - The requested resource could not be found.</summary>
        NotFound = 404,

        /// <summary>408 Request Timeout - The server or client timed out waiting for the request.</summary>
        Timeout = 408,

        /// <summary>429 Too Many Requests - The user has sent too many requests in a given amount of time.</summary>
        TooManyRequests = 429,

        /// <summary>500 Internal Server Error - A generic error occurred on the server.</summary>
        ServerError = 500,

        /// <summary>503 Service Unavailable - The server is temporarily unable to handle the request.</summary>
        ServiceUnavailable = 503,

        /// <summary>520 Unknown Error - An unknown error occurred.</summary>
        UnknownError = 520
    }
}
