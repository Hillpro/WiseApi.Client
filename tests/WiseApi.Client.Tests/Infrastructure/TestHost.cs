using WiseApi.Client.Http;

namespace WiseApi.Client.Tests.Infrastructure;

internal static class TestHost
{
    public static (WiseHttpClient Client, StubHttpMessageHandler Handler) CreateHttpClient()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.wise-sandbox.com"),
        };
        return (new WiseHttpClient(http), handler);
    }
}
