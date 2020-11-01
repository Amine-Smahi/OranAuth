using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OranAuth.Common;
using OranAuth.DomainClasses;

namespace OranAuth.Services
{
    public class JwtTokensData
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenSerial { get; set; }
        public IEnumerable<Claim> Claims { get; set; }
    }

    public interface ITokenFactoryService
    {
        Task<JwtTokensData> CreateJwtTokensAsync(User user);
        string GetRefreshTokenSerial(string refreshTokenValue);
    }

    public class TokenFactoryService : ITokenFactoryService
    {
        private readonly IOptionsSnapshot<BearerTokensOptions> _configuration;
        private readonly ILogger<TokenFactoryService> _logger;
        private readonly IRolesService _rolesService;
        private readonly ISecurityService _securityService;

        public TokenFactoryService(
            ISecurityService securityService,
            IRolesService rolesService,
            IOptionsSnapshot<BearerTokensOptions> configuration,
            ILogger<TokenFactoryService> logger)
        {
            _securityService = securityService;
            _securityService.CheckArgumentIsNull(nameof(_securityService));

            _rolesService = rolesService;
            _rolesService.CheckArgumentIsNull(nameof(rolesService));

            _configuration = configuration;
            _configuration.CheckArgumentIsNull(nameof(configuration));

            _logger = logger;
            _logger.CheckArgumentIsNull(nameof(logger));
        }

        public async Task<JwtTokensData> CreateJwtTokensAsync(User user)
        {
            var (accessToken, claims) = await createAccessTokenAsync(user);
            var (refreshTokenValue, refreshTokenSerial) = createRefreshToken();
            return new JwtTokensData
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                RefreshTokenSerial = refreshTokenSerial,
                Claims = claims
            };
        }

        public string GetRefreshTokenSerial(string refreshTokenValue)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenValue)) return null;

            ClaimsPrincipal decodedRefreshTokenPrincipal = null;
            try
            {
                decodedRefreshTokenPrincipal = new JwtSecurityTokenHandler().ValidateToken(
                    refreshTokenValue,
                    new TokenValidationParameters
                    {
                        RequireExpirationTime = true,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Value.Key)),
                        ValidateIssuerSigningKey = true, // verify signature to avoid tampering
                        ValidateLifetime = true, // validate the expiration
                        ClockSkew = TimeSpan.Zero // tolerance for the expiration date
                    },
                    out _
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to validate refreshTokenValue: `{refreshTokenValue}`.");
            }

            return decodedRefreshTokenPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.SerialNumber)?.Value;
        }

        private (string RefreshTokenValue, string RefreshTokenSerial) createRefreshToken()
        {
            var refreshTokenSerial = _securityService.CreateCryptographicallySecureGuid().ToString().Replace("-", "");

            var claims = new List<Claim>
            {
                // Unique Id for all Jwt tokes
                new Claim(JwtRegisteredClaimNames.Jti, _securityService.CreateCryptographicallySecureGuid().ToString(),
                    ClaimValueTypes.String, _configuration.Value.Issuer),
                // Issuer
                new Claim(JwtRegisteredClaimNames.Iss, _configuration.Value.Issuer, ClaimValueTypes.String,
                    _configuration.Value.Issuer),
                // Issued at
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64, _configuration.Value.Issuer),
                // for invalidation
                new Claim(ClaimTypes.SerialNumber, refreshTokenSerial, ClaimValueTypes.String,
                    _configuration.Value.Issuer)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Value.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                _configuration.Value.Issuer,
                _configuration.Value.Audience,
                claims,
                now,
                now.AddMinutes(_configuration.Value.RefreshTokenExpirationMinutes),
                creds);
            var refreshTokenValue = new JwtSecurityTokenHandler().WriteToken(token);
            return (refreshTokenValue, refreshTokenSerial);
        }

        private async Task<(string AccessToken, IEnumerable<Claim> Claims)> createAccessTokenAsync(User user)
        {
            var claims = new List<Claim>
            {
                // Unique Id for all Jwt tokes
                new Claim(JwtRegisteredClaimNames.Jti, _securityService.CreateCryptographicallySecureGuid().ToString(),
                    ClaimValueTypes.String, _configuration.Value.Issuer),
                // Issuer
                new Claim(JwtRegisteredClaimNames.Iss, _configuration.Value.Issuer, ClaimValueTypes.String,
                    _configuration.Value.Issuer),
                // Issued at
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64, _configuration.Value.Issuer),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(), ClaimValueTypes.String,
                    _configuration.Value.Issuer),
                new Claim(ClaimTypes.Name, user.Username, ClaimValueTypes.String, _configuration.Value.Issuer),
                new Claim("DisplayName", user.DisplayName, ClaimValueTypes.String, _configuration.Value.Issuer),
                // to invalidate the cookie
                new Claim(ClaimTypes.SerialNumber, user.SerialNumber, ClaimValueTypes.String,
                    _configuration.Value.Issuer),
                // custom data
                new Claim(ClaimTypes.UserData, user.Id.ToString(), ClaimValueTypes.String, _configuration.Value.Issuer)
            };

            // add roles
            var roles = await _rolesService.FindUserRolesAsync(user.Id);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role.Name, ClaimValueTypes.String, _configuration.Value.Issuer));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Value.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                _configuration.Value.Issuer,
                _configuration.Value.Audience,
                claims,
                now,
                now.AddMinutes(_configuration.Value.AccessTokenExpirationMinutes),
                creds);
            return (new JwtSecurityTokenHandler().WriteToken(token), claims);
        }
    }
}
