using Moq;
using Moq.Protected;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Test.Util
{
    public static class HttpMessageHandlerMockExtensions
    {
        public class HttpRequestSetup
        {
            private readonly Mock<HttpMessageHandler> _mock;
            private readonly HttpMethod _method;
            private readonly string _url;
            private readonly Queue<HttpResponseMessage> _responses = new();

            public HttpRequestSetup(Mock<HttpMessageHandler> mock, HttpMethod method, string url)
            {
                _mock = mock ?? throw new ArgumentNullException(nameof(mock));
                _method = method ?? throw new ArgumentNullException(nameof(method));
                _url = url ?? throw new ArgumentNullException(nameof(url));

                _mock
                .SetupRequest(_method, _url)
                .ReturnsAsync(() => _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek());
            }

            public HttpRequestSetup ReturnsResponse(HttpStatusCode statusCode, string content, string mediaType = "application/json")
            {
                if (string.IsNullOrEmpty(content))
                {
                    throw new ArgumentException("Response content cannot be null or empty.", nameof(content));
                }

                var response = new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, mediaType)
                };

                _responses.Enqueue(response);
                return this;
            }

        }

        public static HttpRequestSetup SetupSequenceRequest(this Mock<HttpMessageHandler> mock, HttpMethod method, string url)
        {
            return new HttpRequestSetup(mock, method, url);
        }
    }
}
