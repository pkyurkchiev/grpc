using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using StockBroker.gRPC;

namespace SB.Server.Services
{
    public class StockDataService : StocksService.StocksServiceBase
    {
        private readonly ILogger<StockDataService> _logger;
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

        public StockDataService(ILogger<StockDataService> logger)
        {
            _logger = logger;
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
