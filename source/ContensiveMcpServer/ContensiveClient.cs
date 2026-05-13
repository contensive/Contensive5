using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
//
namespace ContensiveMcpServer;
//
public class ContensiveClient {
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    //
    public ContensiveClient(string baseUrl, string bearerToken) {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient();
        if (!string.IsNullOrEmpty(bearerToken)) {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
    //
    public async Task<JsonDocument> GetAsync(string endpoint, Dictionary<string, string>? queryParams = null) {
        if (string.IsNullOrEmpty(_baseUrl)) {
            throw new InvalidOperationException("Contensive BaseUrl is not configured. Set 'Contensive:BaseUrl' in appsettings.json or the CONTENSIVE_BASEURL environment variable.");
        }
        string url = $"{_baseUrl}/{endpoint}";
        if (queryParams != null && queryParams.Count > 0) {
            string query = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            url += $"?{query}";
        }
        var response = await _http.GetAsync(url);
        string json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new Exception($"Contensive API returned HTTP {(int)response.StatusCode} from {url}. Body: {(string.IsNullOrWhiteSpace(json) ? "(empty)" : json)}");
        }
        if (string.IsNullOrWhiteSpace(json)) {
            throw new Exception($"Contensive API returned an empty response from {url} (HTTP {(int)response.StatusCode}).");
        }
        return JsonDocument.Parse(json);
    }
    //
    public async Task<JsonDocument> PostAsync(string endpoint, object body) {
        if (string.IsNullOrEmpty(_baseUrl)) {
            throw new InvalidOperationException("Contensive BaseUrl is not configured. Set 'Contensive:BaseUrl' in appsettings.json or the CONTENSIVE_BASEURL environment variable.");
        }
        string url = $"{_baseUrl}/{endpoint}";
        string json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        string responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            throw new Exception($"Contensive API returned HTTP {(int)response.StatusCode} from {url}. Body: {(string.IsNullOrWhiteSpace(responseJson) ? "(empty)" : responseJson)}");
        }
        if (string.IsNullOrWhiteSpace(responseJson)) {
            throw new Exception($"Contensive API returned an empty response from {url} (HTTP {(int)response.StatusCode}).");
        }
        return JsonDocument.Parse(responseJson);
    }
    //
    /// <summary>
    /// Returns the JSON string from a response document, formatted for Claude to read.
    /// </summary>
    public static string FormatResponse(JsonDocument doc) {
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }
}
