using System.Text.Json.Serialization;

namespace OranAuth.Tests.Integration.Models
{
    public class Token
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
    }
}