namespace EmbeddingService.IntegrationTests
{
    public sealed class DefaultHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly Lazy<HttpMessageHandler> _handlerLazy = new(() => new HttpClientHandler());

        public HttpClient CreateClient(string name)
        {
            var client = new HttpClient(_handlerLazy.Value, disposeHandler: false)
            {
                BaseAddress = new Uri(TestConfiguration.Instance.WebServerUrl)
            };
            return client;
        }

        public void Dispose()
        {
            if (_handlerLazy.IsValueCreated)
            {
                _handlerLazy.Value.Dispose();
            }
        }
    }
}
