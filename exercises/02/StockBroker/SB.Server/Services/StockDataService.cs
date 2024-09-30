using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using SB.Server.Authentications;
using StockBroker.gRPC;
using System.Threading.Channels;

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
            { new() { StockId = "CSCO", StockName = "Cisco Systems Inc" } },
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

        public override async Task<StocksPricesResponse> GetStocksPrices(IAsyncStreamReader<StockViewModel> requestStream, ServerCallContext context)
        {
            Random rnd = new(100);
            List<StockViewModel> inputStocks = [];
            await foreach (var request in requestStream.ReadAllAsync())
            {
                inputStocks.Add(_stocks.First(x => x.StockId == request.StockId));
                _logger.LogInformation("Getting stock Price {StockId}", request.StockId);
            }

            StocksPricesResponse response = new();
            foreach (var inputStock in inputStocks)
            {
                response.Stocks.Add(new StockPriceResponse()
                {
                    Stock = inputStock,
                    DateTimeStamp = DateTime.UtcNow.ToTimestamp(),
                    Price = rnd.Next(100, 600).ToString(),
                });
            }

            return response;
        }

        public override async Task GetCompanyStockPriceStream(IAsyncStreamReader<StockViewModel> requestStream, IServerStreamWriter<StockPriceResponse> responseStream, ServerCallContext context)
        {
            // we'll use a channel here to handle in-process 'messages' concurrently being written to and read from the channel.
            var channel = Channel.CreateUnbounded<StockPriceResponse>();

            // background task which uses async streams to write each stockPrice from the channel to the response steam.
            _ = Task.Run(async () =>
            {
                await foreach (var stockPrice in channel.Reader.ReadAllAsync())
                {
                    await responseStream.WriteAsync(stockPrice);
                }
            });

            // a list of tasks handling requests concurrently
            List<Task> getCompanyStockPriceStreamRequestTasks = [];

            try
            {
                // async streams used to read and process each request from the stream as they are receieved
                await foreach (var request in requestStream.ReadAllAsync())
                {
                    _logger.LogInformation("Getting stock Price for {StockId}", request.StockId);
                    // start and add the request handling task
                    getCompanyStockPriceStreamRequestTasks.Add(GetStockPriceAsync(request));
                }

                _logger.LogInformation("Client finished streaming");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred");
            }

            // wait for all responses to be written to the channel 
            // from the concurrent tasks handling each request
            await Task.WhenAll(getCompanyStockPriceStreamRequestTasks);

            channel.Writer.TryComplete();

            //  wait for all responses to be read from the channel and streamed as responses
            await channel.Reader.Completion;

            _logger.LogInformation("Completed response streaming");

            // a local function which defines a task to handle a Company Stock Price request
            // it mimics 10 consecutive stock price, simulating a delay of 0.5s
            // multiple instances of this will run concurrently for each recieved request
            async Task GetStockPriceAsync(StockViewModel stock)
            {
                Random rnd = new(100);
                for (int i = 0; i < 10; i++)
                {
                    var time = DateTime.UtcNow.ToTimestamp();
                    await channel.Writer.WriteAsync(new()
                    {
                        Stock = _stocks.FirstOrDefault(x => x.StockId == stock.StockId),
                        Price = rnd.Next(100, 500).ToString(),
                        DateTimeStamp = time
                    });

                    await Task.Delay(500);
                }
            }
        }
    }
}
