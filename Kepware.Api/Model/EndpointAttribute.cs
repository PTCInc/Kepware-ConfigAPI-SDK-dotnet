using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EndpointAttribute : Attribute
    {
        /// <summary>
        /// Represents the base endpoint template for the entity type.
        /// </summary>
        public string EndpointTemplate { get; }

        /// <summary>
        /// Represents the suffix to append to the endpoint. This is optional and is often used
        /// when entity types are collection types in the Kepware Configuration API, like tags or 
        /// tag groups can have dynamic endpoints based on their parent entity.
        /// </summary>
        public string? Suffix { get; } = null;

        public EndpointAttribute(string endpointTemplate, string? suffix = default)
        {
            EndpointTemplate = endpointTemplate;
            Suffix = suffix;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RecursiveEndpointAttribute : EndpointAttribute
    {
        /// <summary>
        /// Represents the endpoint template for the recursive or dynamically 
        /// generated endpoints based on their parent entity. Examples would include tags and tag groups.
        /// </summary>
        public string RecursiveEnd { get; }

        /// <summary>
        /// Represents the type of the owner that is used to resolve the recursive endpoint.
        /// Example: if the entity is a tag group, the endpoint resolution would need to
        /// recursively/dynamically resolve if it's parent is a tag group as well. 
        /// </summary>
        public Type RecursiveOwnerType { get; }

        public RecursiveEndpointAttribute(string endpointTemplate, string recursiveEnd, Type recursiveOwnerType, string? suffix = default)
            : base(endpointTemplate, suffix)
        {
            RecursiveEnd = recursiveEnd;
            RecursiveOwnerType = recursiveOwnerType;
        }
    }
}
