using Infinity.Toolkit.Handlers;

namespace Infinity.Toolkit.Tests.Handlers;

public class RequestHandlerTests : TestBase
{
    [Fact]
    public async Task Should_Handle_Request_Correctly()
    {
        // Arrange
        var serviceProvider = ConfigureServiceProvider(services =>
        {
            services.AddRequestHandler<TestRequest, TestResponse, SuccesfulRequestHandler>();
        });

        // Act
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TestRequest, TestResponse>>();

        var response = await handler.HandleAsync(
            HandlerContextExtensions.Create(new TestRequest { RequestData = "Test" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Handled: Test", response.ResponseData);
    }

    [Fact]
    public async Task Should_Fail_Request_Correctly()
    {
        // Arrange
        var serviceProvider = ConfigureServiceProvider(services =>
        {
            services.AddRequestHandler<TestRequest, Result<TestResponse>, FailingRequestHandler>();
        });

        // Act
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TestRequest, Result<TestResponse>>>();

        var response = await handler.HandleAsync(
            HandlerContextExtensions.Create(new TestRequest { RequestData = "Test" }),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBeOfType<ErrorResult<TestResponse>>();
        response.Succeeded.ShouldBeFalse();
        response.Errors.ShouldNotBeEmpty();
        response.Errors.First().Details.ShouldBe("Failed to handle request");
    }

    [Fact]
    public async Task Should_Handle_Request_With_Exception_Error()
    {
        // Arrange
        var serviceProvider = ConfigureServiceProvider(services =>
        {
            services.AddRequestHandler<TestRequest, Result<TestResponse>, FailingWithExceptionRequestHandler>();
        });

        // Act
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TestRequest, Result<TestResponse>>>();
        var response = await handler.HandleAsync(
            HandlerContextExtensions.Create(new TestRequest { RequestData = "" }),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBeOfType<ErrorResult<TestResponse>>();
        response.Errors.First().ShouldBeOfType<ExceptionError>();
        response.Errors.First().Details.ShouldBe("Failed to handle request");
    }
}

public class TestRequest
{
    public string RequestData { get; set; } = string.Empty;
}

public class TestResponse
{
    public string ResponseData { get; set; } = string.Empty;
}

public class SuccesfulRequestHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<TestResponse> HandleAsync(IHandlerContext<TestRequest> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { ResponseData = $"Handled: {context.Request.RequestData}" });
    }
}

public class FailingRequestHandler : IRequestHandler<TestRequest, Result<TestResponse>>
{
    public Task<Result<TestResponse>> HandleAsync(IHandlerContext<TestRequest> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<TestResponse>("Failed to handle request"));
    }
}
public class FailingWithExceptionRequestHandler : IRequestHandler<TestRequest, Result<TestResponse>>
{
    public Task<Result<TestResponse>> HandleAsync(IHandlerContext<TestRequest> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<TestResponse>(new Exception("Failed to handle request")));
    }
}

public static class HandlerContextExtensions
{
    public static HandlerContext<T> Create<T>(T request)
    {
        return new HandlerContext<T>
        {
            Request = request,
            Body = BinaryData.FromObjectAsJson(request)
        };
    }
}
