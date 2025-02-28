using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Kepware.Api.Util
{
    /// <summary>
    /// A custom HttpClientHandler that ensures HttpClient only uses IPv4 addresses for DNS resolution.
    /// </summary>
    public class Ipv4OnlyHttpClientHandler : HttpClientHandler
    {
        /// <summary>
        /// Sends an HTTP request with the specified request message and cancellation token.
        /// Ensures that only IPv4 addresses are used for DNS resolution.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The HTTP response message.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Helper function to check if the address is a valid IPv4 address
            static bool IsValidIPv4(string address)
            {
                return IPAddress.TryParse(address, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork;
            }

            var host = request.RequestUri!.Host;

            // If the host is already an IPv4 address, proceed with the request
            if (IsValidIPv4(host))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

            var ipv4Address = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            // If no IPv4 address is found, throw an exception
            if (ipv4Address == null)
            {
                throw new NotSupportedException($"No IPv4 address found for the hostname {host}.");
            }

            var builder = new UriBuilder(request.RequestUri)
            {
                Host = ipv4Address.ToString()
            };
            
            request.RequestUri = builder.Uri;

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
