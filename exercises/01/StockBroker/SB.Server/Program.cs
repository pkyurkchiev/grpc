using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SB.Server.Authentications;
using SB.Server.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

RegisterServices(builder);

var app = builder.Build();

SetupMiddleware(app);

app.Run();

static void SetupMiddleware(WebApplication webApp)
{
    webApp.UseRouting();
    webApp.UseAuthentication();
    webApp.UseAuthorization();

    // Configure the HTTP request pipeline.
    webApp.MapGrpcService<StockDataService>();
    webApp.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
}

static void RegisterServices(WebApplicationBuilder builder)
{
    // Add services to the container.
    builder.Services.AddGrpc(cfg => cfg.EnableDetailedErrors = true);

    string tokenKey = builder.Configuration["Authentication:TokenKey"] ?? "Not working key";
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<IJWTAuthenticationsManager>(new JWTAuthenticationsManager(tokenKey));
}