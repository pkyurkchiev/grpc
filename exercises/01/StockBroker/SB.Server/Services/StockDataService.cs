using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using SB.Server.Authentications;
using StockBroker.gRPC;

namespace SB.Server.Services
{
    [Authorize]
    public class StockDataService(ILogger<StockDataService> logger, IJWTAuthenticationsManager authManager) : StocksService.StocksServiceBase
    {
        private readonly ILogger<StockDataService> _logger = logger;
        private readonly IJWTAuthenticationsManager _authManager = authManager;
        private readonly List<StockViewModel> _stocks = new()
        {
            { new() { StockId = "META", StockName = "Meta Platforms Inc" } },
            { new() { StockId = "CSCO", StockName = "CSCO" } },
            { new() { StockId = "AAPL", StockName = "Apple Inc" } },
            { new() { StockId = "TSLA", StockName = "Tesla Inc" } },
            { new() { StockId = "PLTR", StockName = "Palantir Technologies Inc" }},
            { new() { StockId = "KO", StockName = "Coca-Cola Co" }},
            { new() { StockId = "PEP", StockName = "PepsiCo Inc" }},
            { new() { StockId = "BMW", StockName = "Bayerische Motoren Werke AG" }},
            { new() { StockId = "AIR", StockName = "Airbus SE" }},
            { new() { StockId = "MC", StockName = "LVMH Moet Hennessy Louis Vuitton SE" } },
        };

        [AllowAnonymous]
        public override Task<AuthenticateResponse> Authenticate(ClientCredentialsRequest request, ServerCallContext context)
        {
            string? token = _authManager.Authenticate(request.ClientId, request.ClientSecret);

            ArgumentNullException.ThrowIfNull(token);

            return Task.FromResult(new AuthenticateResponse() { BearerToken = token });
        }

        public override Task<StocksResponse> GetStocks(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new StocksResponse() { Stocks = { _stocks } });
        }

        public override Task<StockPriceResponse> GetStockPrice(StockViewModel request, ServerCallContext context)
        {
            Random rnd = new(100);
            return Task.FromResult(new StockPriceResponse()
            {
                Stock = _stocks.FirstOrDefault(x => x.StockId == request.StockId),
                DateTimeStamp = DateTime.UtcNow.ToTimestamp(),
                Price = rnd.Next(100, 500).ToString(),
            });
        }

        public override async Task GetStockPriceStream(Empty request, IServerStreamWriter<StockPriceResponse> responseStream, ServerCallContext context)
        {
            Random rnd = new(100);
            while (!context.CancellationToken.IsCancellationRequested)
            {
                _stocks.ForEach(async stock =>
                {
                    var time = DateTime.UtcNow.ToTimestamp();
                    await responseStream.WriteAsync(new StockPriceResponse 
                    {
                        Stock = stock,
                        DateTimeStamp = time,
                        Price = rnd.Next(100, 500).ToString(),
                    });
                });

                await Task.Delay(300);
            }
        }
    }
}
