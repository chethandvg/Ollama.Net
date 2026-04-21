using System.Net;
using Microsoft.Extensions.Options;

namespace Krutaka.Ollama.Configuration;

/// <summary>
/// Validates <see cref="OllamaClientOptions"/> configuration.
/// </summary>
internal sealed class OllamaClientOptionsValidator : IValidateOptions<OllamaClientOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, OllamaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.BaseAddress is null)
        {
            return ValidateOptionsResult.Fail("BaseAddress must not be null.");
        }

        if (!options.BaseAddress.IsAbsoluteUri)
        {
            return ValidateOptionsResult.Fail("BaseAddress must be an absolute URI.");
        }

        if (options.BaseAddress.Scheme != "http" && options.BaseAddress.Scheme != "https")
        {
            return ValidateOptionsResult.Fail(
                $"BaseAddress scheme '{options.BaseAddress.Scheme}' is not supported. Use 'http' or 'https'.");
        }

        if (options.BaseAddress.Scheme == "http")
        {
            string host = options.BaseAddress.Host;
            bool isLoopback = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                              (IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address));

            if (!isLoopback && !options.AllowInsecureHttp)
            {
                return ValidateOptionsResult.Fail(
                    "HTTP connections to non-loopback addresses are not allowed unless AllowInsecureHttp is set to true. " +
                    "Use HTTPS for production deployments.");
            }
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail("Timeout must be greater than zero.");
        }

        if (options.MaxRetries < 0 || options.MaxRetries > 10)
        {
            return ValidateOptionsResult.Fail("MaxRetries must be between 0 and 10.");
        }

        return ValidateOptionsResult.Success;
    }
}
