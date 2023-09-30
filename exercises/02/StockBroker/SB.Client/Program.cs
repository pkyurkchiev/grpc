using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using StockBroker.gRPC;
using System.Reflection.PortableExecutable;
using static StockBroker.gRPC.StocksService;

namespace SB.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using var channel = GrpcChannel.ForAddress("http://localhost:5082");
            StocksServiceClient client = new(channel);
            Metadata headers = await FetchAuhtenticationToken(client);

            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("*****************************************");
                    Console.WriteLine("Press 1 to GetStocks");
                    Console.WriteLine("Press 2 to GetStockPrice");
                    Console.WriteLine("Press 3 to GetStockPriceStream");
                    Console.WriteLine("Press 4 to GetStocksPrices");
                    Console.WriteLine("Press 5 to GetCompanyStockPriceStream");
                    Console.WriteLine("*****************************************");
                    Console.ResetColor();

                    var input = Console.ReadLine();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    switch (input)
                    {
                        case "1":
                            await GetStocks(client, headers);
                            break;
                        case "2":
                            await GetStockPrice(client, headers);
                            break;
                        case "3":
                            await GetStockPriceAsync(client, headers);
                            break;
                        case "4":
                            await GetStocksPrices(client, headers);
                            break;
                        case "5":
                            await GetCompanyStockPriceStream(client, headers);
                            break;
                        default:
                            Environment.Exit(0);
                            break;
                    }
                    Console.ResetColor();
                }
                catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Unauthenticated)
                {
                    Console.WriteLine("Token expired/rejected. Rejecting latest...");
                    headers = await FetchAuhtenticationToken(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in application: {ex.Message}");
                }
            }
        }

        private static async Task<Metadata> FetchAuhtenticationToken(StocksServiceClient client)
        {
            var authToken = await client.AuthenticateAsync(new() { ClientId = "fmi", ClientSecret = "fmi" });
            return new Metadata
            {
                { "Authorization", $"Bearer {authToken.BearerToken}"  }
            };
        }

        private static async Task GetStocks(StocksServiceClient client, Metadata headers)
        {
            var result = await client.GetStocksAsync(new Empty(), headers);
            foreach (var stock in result.Stocks)
            {
                Console.WriteLine($"{stock.StockId}\t{stock.StockName}");
            }
        }

        private static async Task GetStockPrice(StocksServiceClient client, Metadata headers)
        {
            var result = await client.GetStockPriceAsync(new() { StockId = "BMW" }, headers);
            Console.WriteLine($"Stock Id: {result.Stock.StockId}\nStockName: {result.Stock.StockName}\nStock Price: {result.Price}\nTimeStamp: {result.DateTimeStamp.ToDateTime()}");
        }

        private static async Task GetStockPriceAsync(StocksServiceClient client, Metadata headers)
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            using var streamingCall = client.GetStockPriceStream(new Empty(), cancellationToken: cancellationToken.Token, headers: headers);

            try
            {
                Console.WriteLine($"Stock Id\t Stock Name\t Stock Price\t TimeStamp");
                await foreach (var message in streamingCall.ResponseStream.ReadAllAsync(cancellationToken.Token))
                {
                    Console.WriteLine($"{message.Stock.StockId}\t{message.Stock.StockName}\t{message.Price}\t{message.DateTimeStamp}");
                }
            }
            catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Stream cancelling.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading response: {ex.Message}");
            }
        }

        private static async Task GetStocksPrices(StocksServiceClient client, Metadata headers)
        {
            using var streamingCall = client.GetStocksPrices(headers);
            foreach (var stockId in new[] { "CSCO", "PLTR", "KO", "BMW", "META" })
            {
                Console.WriteLine($"Requesting details from {stockId}...");

                await streamingCall.RequestStream.WriteAsync(new() { StockId = stockId });

                await Task.Delay(1000);
            }

            Console.WriteLine("Completing request stream.");
            await streamingCall.RequestStream.CompleteAsync();
            Console.WriteLine("Request stream completed.");

            var response = await streamingCall;
            Console.WriteLine($"Stock Id\t Stock Name\t Stock Price\t TimeStamp");
            foreach (var message in response.Stocks)
            {
                Console.WriteLine($"{message.Stock.StockId}\t{message.Stock.StockName}\t{message.Price}\t{message.DateTimeStamp}");
            }
            Console.WriteLine();
        }

        private static async Task GetCompanyStockPriceStream(StocksService.StocksServiceClient client, Metadata headers)
        {
            //To get all stock Ids
            //var stocks = client.GetStockListings(new Empty()).Stocks; 
            using var streamingCall = client.GetCompanyStockPriceStream(headers: headers);

            // background task which uses async streams to read each stockPrice from the response steam.
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var stockPrice in streamingCall.ResponseStream.ReadAllAsync())
                    {
                        Console.WriteLine($"{stockPrice.Stock.StockId}\t {stockPrice.Stock.StockName}\t {stockPrice.Price}\t {stockPrice.DateTimeStamp}");
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("Stream cancelled.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading response: " + ex);
                }
            });

            //send requests through requst stream
            foreach (var stockId in new[] { "CSCO", "PLTR", "KO", "BMW", "META" })
            {
                Console.WriteLine($"Requesting details for {stockId}...");

                await streamingCall.RequestStream.WriteAsync(new StockViewModel { StockId = stockId });

                //Mimicing delay in sending request
                await Task.Delay(1500);
            }

            Console.WriteLine("Completing request stream");
            await streamingCall.RequestStream.CompleteAsync();
            Console.WriteLine("Request stream completed");
        }
    }
}