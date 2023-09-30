using Grpc.Core;

namespace FirstGrpcService.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        public override Task<CalculatorResponse> Calculator(CalculatorRequest request, ServerCallContext context)
        {
            CalculatorResponse result = new()
            {
                Perimeter = "Primeter: " + (request.A * 2 + request.B * 2),
                Area = "Area: " + (request.A * request.B)
            };

            return Task.FromResult(result);
        }

        public override Task<CalculatorResponse> MultiCalculator(MultiCalculatorRequest request, ServerCallContext context)
        {
            CalculatorResponse result = request.Type switch
            {
                CalculatorTypeEnum.Square => new()
                {
                    Perimeter = "Primeter: " + (request.A * 2 + request.B * 2),
                    Area = "Area: " + (request.A * request.B)
                },
                CalculatorTypeEnum.Circle => new()
                {
                    Perimeter = "Primeter: " + (3.14 * request.A * 2),
                    Area = "Area: " + (3.14 * request.A * request.A)
                },
                _ => throw new NotImplementedException()
            };

            return Task.FromResult(result);
        }
    }
}