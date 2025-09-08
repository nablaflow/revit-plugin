using ArchiwindRevitAddIn.Api;
using ArchiWindRevitAddIn.Views;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using System.Security;

namespace ArchiWindRevitAddIn.Services
{
    public static class ServiceLocator
    {
        private static HttpClient? _apiClient;
        private static readonly object _lock = new();

        public static HttpClient ApiClient
        {
            get
            {
                if (_apiClient == null)
                {
                    lock (_lock)
                    {
                        _apiClient ??= CreateApiClient();
                    }
                }

                return _apiClient;
            }
        }

        public static void Initialize()
        {
            lock (_lock)
            {
                _apiClient = CreateApiClient();
            }
        }

        public static HttpClient CreateApiClient(SecureString? pat = null)
        {
            return CreateApiClient(pat, GetBaseUrl());
        }

        private static HttpClient CreateApiClient(SecureString? pat, string? baseUrl)
        {
            var httpClient = KiotaClientFactory.Create(
                finalHandler: new System.Net.Http.HttpClientHandler(),
                handlers: [
                    new IdempotencyKeyHandler(),
                    new BodyInspectionHandler(new()
                    {
                        InspectRequestBody = true,
                        InspectResponseBody = true,
                    }),
                    new RetryHandler(new()
                    {
                        RetriesTimeLimit = TimeSpan.FromSeconds(30),
                    }),
                    new UserAgentHandler(new()
                    {
                        Enabled = true,
                        ProductName = "revit-plugin",
                    }),
                ]
            );

            var authProvider = new PersonalAccessTokenAuthenticationProvider(pat);

            var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);

            if (baseUrl != null)
            {
                requestAdapter.BaseUrl = baseUrl;
            }

            return new HttpClient(requestAdapter);
        }

        private static string? GetBaseUrl()
        {
            return Environment.GetEnvironmentVariable("ARCHIWIND_BASEURL");
        }

        public static void Dispose()
        {
            lock (_lock)
            {
                _apiClient = null;
            }
        }
    }

    public class PersonalAccessTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly SecureString? pat;

        public PersonalAccessTokenAuthenticationProvider(SecureString? pat = null)
        {
            this.pat = pat;
        }

        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var pat = this.pat ?? ConfigurationService.RetrievePAT();

            if (pat == null)
            {
                return Task.FromException(new InvalidOperationException("no PAT configured"));
            }

            request.Headers.TryAdd("x-nablaflow-token", Utils.ConvertSecureStringToString(pat));

            return Task.CompletedTask;
        }
    }

    public class IdempotencyKeyHandler : System.Net.Http.DelegatingHandler
    {
        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Method == System.Net.Http.HttpMethod.Post)
            {
                request.Headers.Add("idempotency-key", Guid.NewGuid().ToString());
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
