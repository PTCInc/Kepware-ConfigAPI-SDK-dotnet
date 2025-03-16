using Kepware.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kepware.Api.Util
{
    public  static partial class EndpointResolver
    {
        private static readonly Regex s_pathplaceHolderRegex = EndpointPlaceholderRegex();

        #region ResolveEndpoint
        public static string ResolveEndpoint<T>()
            => ResolveEndpoint<T>([]);
        /// <summary>
        /// Resolves the endpoint for the specified entity type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="owner"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ResolveEndpoint<T>(NamedEntity? owner, string? itemName = default)
        {
            var endpointAttr = typeof(T)
                .GetCustomAttributes(typeof(EndpointAttribute), false)
                .OfType<EndpointAttribute>()
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No endpoint defined for {typeof(T).Name}");

            var ownerNames = owner?.Flatten().Select(n => n.Name).Reverse().ToArray() ?? Array.Empty<string>();
            var recursiveAttr = endpointAttr as RecursiveEndpointAttribute;

            return (string.IsNullOrEmpty(itemName), string.IsNullOrEmpty(endpointAttr.Suffix), recursiveAttr != null, recursiveAttr?.RecursiveOwnerType == owner?.GetType()) switch
            {
                // Recursive case: owner type matches and itemName exists
                (false, true, true, true)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames) + s_pathplaceHolderRegex.Replace(recursiveAttr!.RecursiveEnd, itemName!),

                // Recursive case: owner type matches, no itemName
                (true, _, true, true)
                    => ResolveRecursiveEndpoint(recursiveAttr!, owner) + (endpointAttr.Suffix ?? string.Empty),

                // Recursive case: suffix exists, no itemName, owner type mismatch
                (true, _, true, false)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames) + (endpointAttr.Suffix ?? string.Empty),

                // Recursive case: suffix exists and itemName present, owner type mismatch
                (false, false, true, _) when s_pathplaceHolderRegex.IsMatch(endpointAttr.Suffix!)
                    => ResolveRecursiveEndpoint(recursiveAttr!, owner) + s_pathplaceHolderRegex.Replace(endpointAttr.Suffix!, itemName!),

                // Recursive case: recursive endpoint template has placeholders and itemName present
                (false, true, true, false) when !string.IsNullOrEmpty(recursiveAttr?.RecursiveEnd) && s_pathplaceHolderRegex.IsMatch(recursiveAttr.RecursiveEnd)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames) + s_pathplaceHolderRegex.Replace(recursiveAttr.RecursiveEnd, itemName!),

                (false, true, true, false)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames).TrimEnd('/') + '/' + itemName!,

                // General case: itemName exists and placeholder replacement is needed in suffix
                (false, false, false, _) when s_pathplaceHolderRegex.IsMatch(endpointAttr.Suffix!)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames) + s_pathplaceHolderRegex.Replace(endpointAttr.Suffix!, itemName!),

                // General case: itemName exists, suffix present but no placeholders
                (false, _, false, _)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames.Append(itemName!).ToArray()) + (endpointAttr.Suffix ?? string.Empty),

                // General case: no itemName, return endpoint with suffix
                (true, _, false, _)
                    => ReplacePlaceholders(endpointAttr.EndpointTemplate, ownerNames) + endpointAttr.Suffix,

                // Recursive case: itemName present, but recursive endpoint does not support it
                (false, false, true, _)
                    => throw new InvalidOperationException("Recursive endpoint does not support item name"),
            };
        }

        /// <summary>
        /// Resolves the endpoint for the specified entity type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="placeholderValues"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ResolveEndpoint<T>(IEnumerable<string> placeholderValues)
        {
            var endpointTemplateAttribute = typeof(T).GetCustomAttributes(typeof(EndpointAttribute), false)
                .OfType<EndpointAttribute>()
                .FirstOrDefault();

            if (endpointTemplateAttribute == null)
            {
                throw new InvalidOperationException($"No endpoint defined for {typeof(T).Name}");
            }

            if (endpointTemplateAttribute is RecursiveEndpointAttribute)
            {
                throw new InvalidOperationException("Recursive endpoint does not support string list item name");
            }

            return ReplacePlaceholders(endpointTemplateAttribute.EndpointTemplate, placeholderValues) + endpointTemplateAttribute.Suffix;
        }

        public static string ReplacePlaceholders(string template, IEnumerable<string> placeholderValues)
        {
            var placeholders = s_pathplaceHolderRegex.Matches(template).ToArray();
            var values = placeholderValues.ToArray();
            if (placeholders.Length != values.Length)
            {
                throw new InvalidOperationException($"The number of placeholders in the template '{template}' does not match the number of values ({string.Join(",", values)}).");
            }

            foreach (var match in placeholders.Zip(values, (placeholder, value) => (placeholder, value)))
            {
                template = template.Replace(match.placeholder.Value, Uri.EscapeDataString(match.value));
            }

            return template;
        }

        private static string ResolveRecursiveEndpoint(RecursiveEndpointAttribute attribute, NamedEntity? owner)
        {
            LinkedList<string> recursivePath = new LinkedList<string>();
            while (owner != null && attribute.RecursiveOwnerType == owner?.GetType())
            {
                var currentEndpointPart = ReplacePlaceholders(attribute.RecursiveEnd, [owner.Name]);
                recursivePath.AddFirst(currentEndpointPart);

                if (owner is IHaveOwner ownable && ownable.Owner is NamedEntity nextOwner)
                    owner = nextOwner;
                else
                    owner = null;
            }

            // Combine with the base endpoint template 
            var baseEndpoint = ReplacePlaceholders(attribute.EndpointTemplate, owner?.Flatten().Select(n => n.Name).Reverse() ?? []);

            return baseEndpoint + string.Concat(recursivePath);
        }

        [GeneratedRegex(@"\{(.+?)\}", RegexOptions.Compiled)]
        private static partial Regex EndpointPlaceholderRegex();
        #endregion
    }
}
