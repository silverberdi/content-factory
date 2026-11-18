using System.Net.Http;

namespace ContentFactory.Api.Tests;

public class TestHttpClientFactory : IHttpClientFactory
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;

    public TestHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage>? handler = null)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name = "")
    {
        if (_handler != null)
        {
            return new HttpClient(new MockHttpMessageHandler(_handler));
        }
        return new HttpClient();
    }

    private class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
