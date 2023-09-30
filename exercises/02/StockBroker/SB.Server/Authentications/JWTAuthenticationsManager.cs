using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SB.Server.Authentications
{
    public class JWTAuthenticationsManager : IJWTAuthenticationsManager
    {
        private readonly Dictionary<string, string> clients = new()
        {
            { "fmi", "fmi" },
            { "fmi2", "fmi2" },
        };

        private readonly string tokenKey;

        public JWTAuthenticationsManager(string tokenKey)
        {
            this.tokenKey = tokenKey;
        }

        public string? Authenticate(string clientId, string secret)
        {
            if (!clients.Any(x => x.Key == clientId && x.Value == secret))
            {
                return null;
            }

            JwtSecurityTokenHandler tokenHandler = new();
            var key = Encoding.ASCII.GetBytes(tokenKey);
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, clientId)
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
