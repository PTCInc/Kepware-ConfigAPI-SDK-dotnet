using Kepware.Api.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents the result of an API call.
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Gets or sets the status code of the API result.
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the message of the API result.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets the HTTP status code corresponding to the API result code.
        /// </summary>
        [JsonIgnore]
        public HttpStatusCode HttpStatusCode => (HttpStatusCode)Code;

        /// <summary>
        /// Gets a value indicating whether the status code is in the range 200-299.
        /// </summary>
        [JsonIgnore]
        public bool IsSuccessStatusCode => Code >= 200 && Code < 300;
    }

    /// <summary>
    /// Represents the status of the Configuration API REST service.
    /// </summary>
    public class ApiStatus
    {
        /// <summary>
        /// Gets or sets the name of the server being checked.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the service is running.
        /// </summary>
        public bool Healthy { get; set; } = false;
    }

    /// <summary>
    /// Represents the type of product.
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Unknown product type.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Kepware Edge - This covers Edge branded products including Thingworx Kepware Edge.
        /// </summary>
        KepwareEdge = 13,
        /// <summary>
        /// Kepware Server - This covers all Kepware branded products including KEPServerEX,
        /// Thingworx Kepware Server, and future products that use the Kepware Server codebase.
        /// </summary>
        KepwareServer = 12
    }

    /// <summary>
    /// Represents information about a product.
    /// </summary>
    public class ProductInfo
    {
        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product version.
        /// </summary>
        [JsonPropertyName("product_version")]
        public string ProductVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the major version of the product.
        /// </summary>
        [JsonPropertyName("product_version_major")]
        public int ProductVersionMajor { get; set; }

        /// <summary>
        /// Gets or sets the minor version of the product.
        /// </summary>
        [JsonPropertyName("product_version_minor")]
        public int ProductVersionMinor { get; set; }

        /// <summary>
        /// Gets or sets the build version of the product.
        /// </summary>
        [JsonPropertyName("product_version_build")]
        public int ProductVersionBuild { get; set; }

        /// <summary>
        /// Gets or sets the patch version of the product.
        /// </summary>
        [JsonPropertyName("product_version_patch")]
        public int ProductVersionPatch { get; set; }

        /// <summary>
        /// Gets the type of the product.
        /// </summary>
        public ProductType ProductType
        {
            get
            {
                if (int.TryParse(ProductId, out var id))
                {
                    var prodType = (ProductType)id;
                    if (Enum.IsDefined(prodType))
                        return prodType;
                    else
                        return ProductType.Unknown;
                }
                else if (Enum.TryParse<ProductType>(ProductName, out var prodType))
                    return prodType;
                else
                    return ProductType.Unknown;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the product supports JSON project load service.
        /// </summary>
        [JsonIgnore]
        public bool SupportsJsonProjectLoadService =>
            // Supported in KEPServerEX 6.17+, ThingWorx Kepware Server 6.17+, Kepware Edge, and ThingWorx Kepware Edge 1.10+
            // Kepware Edge and ThingWorx Kepware Edge use the same ProductType and are differentiated by ProductName
            (ProductType == ProductType.KepwareServer && (ProductVersionMajor > 6 || (ProductVersionMajor == 6 && ProductVersionMinor >= 17))) ||
            (ProductType == ProductType.KepwareEdge && (ProductVersionMajor > 1 || (ProductVersionMajor == 1 && ProductVersionMinor >= 10))) ||
            (ProductType == ProductType.KepwareEdge && ProductName == "Kepware Edge");
        
    }


}
