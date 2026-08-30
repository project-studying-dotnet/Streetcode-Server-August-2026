// <copyright file="ValidationBehaviorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.MediatRTests.Behaviors
{
    using FluentValidation;
    using global::Streetcode.BLL.MediatR.Behaviors;
    using MediatR;
    using Xunit;

    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_WhenRequestIsValid_ShouldCallNext()
        {
            var request = new TestRequest("Valid Name");

            var validators = new IValidator<TestRequest>[]
            {
                new TestRequestValidator(),
            };

            var behavior =
                new ValidationBehavior<TestRequest, string>(validators);

            bool nextCalled = false;

            Task<string> Next(CancellationToken cancellationToken)
            {
                nextCalled = true;
                return Task.FromResult("Handled");
            }

            string result = await behavior.Handle(
                request,
                Next,
                CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Handled", result);
        }

        [Fact]
        public async Task Handle_WhenRequestIsInvalid_ShouldThrowValidationException()
        {
            var request = new TestRequest(string.Empty);

            var validators = new IValidator<TestRequest>[]
            {
                new TestRequestValidator(),
            };

            var behavior =
                new ValidationBehavior<TestRequest, string>(validators);

            bool nextCalled = false;

            Task<string> Next(CancellationToken cancellationToken)
            {
                nextCalled = true;
                return Task.FromResult("Handled");
            }

            ValidationException exception =
                await Assert.ThrowsAsync<ValidationException>(
                    () => behavior.Handle(
                        request,
                        Next,
                        CancellationToken.None));

            Assert.Single(exception.Errors);

            var validationError = exception.Errors.Single();

            Assert.Equal(
                nameof(TestRequest.Name),
                validationError.PropertyName);

            Assert.False(nextCalled);
        }

        [Fact]
        public async Task Handle_WhenTwoValidatorsFail_ShouldCombineErrors()
        {
            var request = new TestRequest(string.Empty);

            var validators = new IValidator<TestRequest>[]
            {
                new TestRequestValidator(),
                new SecondTestRequestValidator(),
            };

            var behavior =
                new ValidationBehavior<TestRequest, string>(validators);

            Task<string> Next(CancellationToken cancellationToken)
            {
                return Task.FromResult("Handled");
            }

            ValidationException exception =
                await Assert.ThrowsAsync<ValidationException>(
                    () => behavior.Handle(
                        request,
                        Next,
                        CancellationToken.None));

            Assert.Equal(2, exception.Errors.Count());
        }

        [Fact]
        public async Task Handle_WhenNoValidators_ShouldCallNext()
        {
            var request = new TestRequest("Valid Name");

            IValidator<TestRequest>[] validators =
                Array.Empty<IValidator<TestRequest>>();

            var behavior =
                new ValidationBehavior<TestRequest, string>(validators);

            bool nextCalled = false;

            Task<string> Next(CancellationToken cancellationToken)
            {
                nextCalled = true;
                return Task.FromResult("Handled");
            }

            string result = await behavior.Handle(
                request,
                Next,
                CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Handled", result);
        }

        private sealed class TestRequest : IRequest<string>
        {
            public TestRequest(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
        }

        private sealed class TestRequestValidator
            : AbstractValidator<TestRequest>
        {
            public TestRequestValidator()
            {
                this.RuleFor(request => request.Name)
                    .NotEmpty();
            }
        }

        private sealed class SecondTestRequestValidator
            : AbstractValidator<TestRequest>
        {
            public SecondTestRequestValidator()
            {
                this.RuleFor(request => request.Name)
                    .NotEmpty()
                    .WithMessage("Name is required by the second validator.");
            }
        }
    }
}