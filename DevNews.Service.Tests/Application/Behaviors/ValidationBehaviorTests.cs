using DevNews.Services.Application.Behaviors;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;

namespace DevNews.Service.Tests.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task GivenValidRequest_WhenHandled_ThenInvokesNextDelegate()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator.Object });
        var nextWasCalled = false;
        RequestHandlerDelegate<string> next = (_) =>
        {
            nextWasCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(new TestRequest("Seattle"), next, CancellationToken.None);

        Assert.True(nextWasCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task GivenInvalidRequest_WhenHandled_ThenThrowsValidationException()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure(nameof(TestRequest.City), "City name is required."),
            }));

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator.Object });
        var nextWasCalled = false;
        RequestHandlerDelegate<string> next = (_) =>
        {
            nextWasCalled = true;
            return Task.FromResult("ok");
        };

        var action = () => behavior.Handle(new TestRequest(string.Empty), next, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ValidationException>(action);
        Assert.False(nextWasCalled);
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(TestRequest.City));
    }

    public sealed record TestRequest(string City) : IRequest<string>;
}


