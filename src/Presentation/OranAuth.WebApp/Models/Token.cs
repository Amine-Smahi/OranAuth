using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OranAuth.WebApp.Models
{
    public class Token
    {
        [JsonPropertyName("refreshToken")]
        [Required]
        public string RefreshToken { get; set; }
    }
}