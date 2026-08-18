using FluentValidation;
using MediatR;
using Streetcode.BLL.MediatR.Behaviors;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.Behaviors
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_WhenRequestIsValid_ShouldCallNext()
        {
            var request = new TestRequest("Valid Name");
            var validators = new IValidator<TestRequest>[]{ new TestRequestValidator(), };
            var behavior = new ValidationBehavior<TestRequest, string>(validators);
            bool nextCalled = false;
            Task<string> Next(CancellationToken cancellationToken)
            {
                nextCalled = true;
                return Task.FromResult("Handled");
            }

            var result = await behavior.Handle(request, Next, CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Handled", result);
        }

        [Fact]
        public async Task Handle_WhenRequestIsInvalid_ShouldThrowValidationException()
        {
            var request = new TestRequest(string.Empty);
            var validators = new IValidator<TestRequest>[]{new TestRequestValidator(), };
            var behavior = new ValidationBehavior<TestRequest, string>(validators);
            bool nextCalled = false;
            Task<string> Next (CancellationToken cancellationToken)
            {
                nextCalled = true;
                return Task.FromResult("Handled");
            }

            var result = await Assert.ThrowsAsync<ValidationException>(
                () => behavior.Handle(request, Next, CancellationToken.None));

            Assert.Single(result.Errors);
            var validationError = result.Errors.Single();
            Assert.Equal(nameof(TestRequest.name), validationError.PropertyName);
            Assert.False(nextCalled);
        }

        private sealed record TestRequest(string name) : IRequest<string>;

        private sealed class TestRequestValidator : AbstractValidator<TestRequest>
        {
            public TestRequestValidator()
            {
                RuleFor(request => request.name).NotEmpty();
            }
        }
    }
}
