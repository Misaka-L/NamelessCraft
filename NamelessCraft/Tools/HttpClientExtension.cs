using System.Net.Http.Json;
using System.Text.Json;

namespace NamelessCraft.Tools;

public static class HttpClientExtension
{
    public static async ValueTask<T> PostAsJsonAsync<T>(this HttpClient httpClient, string requestUri, object value)
    {
        var response = await httpClient.PostAsJsonAsync(requestUri, value, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        });

        return await response.Content.ReadFromJsonAsync<T>() ??
               throw new InvalidOperationException("Can't deserialize the response");
    }

    public static async ValueTask<T> GetAsync<T>(this HttpClient httpClient, string requestUri)
    {
        var response = await httpClient.GetAsync(requestUri);
        
        return await response.Content.ReadFromJsonAsync<T>() ??
               throw new InvalidOperationException("Can't deserialize the response");
    }
}