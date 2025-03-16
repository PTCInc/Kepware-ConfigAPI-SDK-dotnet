using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a structured response from an API call, encapsulating a response code, an optional message, an optional value, and the endpoint reference.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the response.</typeparam>
    public readonly struct ApiResponse<T> : IEquatable<ApiResponse<T>>, IEquatable<T>
    {
        /// <summary>
        /// Gets the response code indicating the status of the API call.
        /// </summary>
        public ApiResponseCode ResponseCode { get; }

        /// <summary>
        /// Gets the optional message describing the result of the API call.
        /// </summary>
        public string? Message { get; }

        /// <summary>
        /// Gets the value returned from the API, if applicable.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Gets the endpoint that was called for this response.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Indicates whether the response represents a successful API call.
        /// </summary>
        public bool IsSuccess => ResponseCode is ApiResponseCode.Success or ApiResponseCode.Created or ApiResponseCode.Accepted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse{T}"/> struct with a response code, value, and endpoint.
        /// </summary>
        /// <param name="responseCode">The response code indicating the status of the API call.</param>
        /// <param name="value">The value returned from the API, if applicable.</param>
        /// <param name="endpoint">The endpoint that was called for this response.</param>
        public ApiResponse(ApiResponseCode responseCode, T value, string endpoint)
        {
            ResponseCode = responseCode;
            Value = value;
            Message = null;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse{T}"/> struct with a response code, message, and endpoint.
        /// </summary>
        /// <param name="responseCode">The response code indicating the status of the API call.</param>
        /// <param name="message">The message describing the result of the API call.</param>
        /// <param name="endpoint">The endpoint that was called for this response.</param>
        public ApiResponse(ApiResponseCode responseCode, string message, string endpoint)
        {
            ResponseCode = responseCode;
            Message = message;
            Value = default;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Ensures that the API call was successful, throwing an exception if it was not.
        /// </summary>
        /// <exception cref="ApiCallFailedException">Thrown if the API call was not successful.</exception>
        public void EnsureSuccess()
        {
            if (!IsSuccess)
                throw GetApiCallFailedException();
        }

        private ApiCallFailedException GetApiCallFailedException() => new(ResponseCode, Endpoint, Message ?? (IsSuccess ? "No value returned from successful API call." : "API call failed without specific message."));

        /// <summary>
        /// Implicitly converts the <see cref="ApiResponse{T}"/> to its value if the response was successful.
        /// </summary>
        /// <param name="response">The API response.</param>
        /// <returns>The value contained in the response.</returns>
        /// <exception cref="InvalidCastException">Thrown if the response was not successful or the value is null.</exception>
        public static implicit operator T(ApiResponse<T> response)
        {
            if (!response.IsSuccess || response.Value is null)
#pragma warning disable S3877 // Exceptions should not be thrown from unexpected methods
                throw new InvalidCastException("Unable to cast API response to its value", response.GetApiCallFailedException());
#pragma warning restore S3877 // Exceptions should not be thrown from unexpected methods

            return response.Value;
        }

        /// <inheritdoc />
        public bool Equals(ApiResponse<T> other) => ResponseCode == other.ResponseCode && Message == other.Message && EqualityComparer<T?>.Default.Equals(Value, other.Value);

        /// <inheritdoc />
        public bool Equals(T? other) => EqualityComparer<T?>.Default.Equals(Value, other);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ApiResponse<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(ResponseCode, Message, Value, Endpoint);

        /// <inheritdoc />
        public static bool operator ==(ApiResponse<T> left, ApiResponse<T> right) => left.Equals(right);

        /// <inheritdoc />
        public static bool operator !=(ApiResponse<T> left, ApiResponse<T> right) => !left.Equals(right);
    }
}
