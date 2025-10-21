namespace DefaultNamespace;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class MailTmClient
{
    private readonly HttpClient _http;
    public MailTmClient(HttpClient http) => _http = http;

    public record DomainItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("domain")] string Domain
    );

    public class HydraPage<T>
    {
        [JsonPropertyName("hydra:member")]
        public List<T> Items { get; set; } = [];
        [JsonPropertyName("hydra:totalItems")]
        public int Total { get; set; }
    }

    public async Task<(List<DomainItem> Items, int Total)> GetDomainsAsync()
    {
        var page = await _http.GetFromJsonAsync<HydraPage<DomainItem>>("domains");
        return (page?.Items ?? [], page?.Total ?? 0);
    }
}