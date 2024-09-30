namespace SB.Server.Authentications
{
    public interface IJWTAuthenticationsManager
    {
        string? Authenticate(string clientId, string secret);
    }
}
