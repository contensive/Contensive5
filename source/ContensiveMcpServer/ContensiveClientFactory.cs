namespace ContensiveMcpServer;
//
/// <summary>
/// Factory for creating per-request ContensiveClient instances.
/// The base URL is configured once; each request provides its own bearer token.
/// </summary>
public class ContensiveClientFactory {
    private readonly string _baseUrl;
    //
    public ContensiveClientFactory(string baseUrl) {
        _baseUrl = baseUrl.TrimEnd('/');
    }
    //
    public ContensiveClient Create(string bearerToken) {
        return new ContensiveClient(_baseUrl, bearerToken);
    }
}
