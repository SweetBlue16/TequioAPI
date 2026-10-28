using Microsoft.Extensions.Logging;
using Moq;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Xunit;

namespace Tequio.Tests.Users;

/// <summary>Tests profile orchestration without database access.</summary>
public sealed class UserServiceTest
{
    private readonly Mock<IUserRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<ILogger<UserService>> _logger = new();

    [Theory]
    [InlineData(null)]
    [InlineData("https://images.example.test/profile.jpg")]
    public async Task TestGetUserProfileShouldReturnRepositoryProfile(string? pictureUrl)
    {
        var profile = new UserProfileDto
        {
            Id = 42,
            Email = "ana@example.test",
            FirstName = "Ana",
            PaternalLastName = "López",
            ProfilePictureUrl = pictureUrl
        };
        _repository.Setup(repository => repository.GetUserProfileAsync(42)).ReturnsAsync(profile);

        var result = await CreateService().GetUserProfileAsync(42);

        Assert.Same(profile, result);
        _repository.Verify(repository => repository.GetUserProfileAsync(42), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestGetUserProfileWithMissingUserShouldPropagateRepositoryError()
    {
        var expected = new KeyNotFoundException(ErrorMessages.RecordNotFound);
        _repository.Setup(repository => repository.GetUserProfileAsync(42)).ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService().GetUserProfileAsync(42));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.GetUserProfileAsync(42), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestUpdateUserProfileShouldDelegateUnchangedRequest(bool includeOptionalFields)
    {
        var request = new UpdateUserProfileDto
        {
            FirstName = " Ana ",
            PaternalLastName = "López",
            BirthDate = new DateTime(1990, 1, 1),
            MaternalLastName = includeOptionalFields ? "Pérez" : null,
            PhoneNumber = includeOptionalFields ? "5551234567" : null,
            Locality = includeOptionalFields ? "Xalapa" : null,
            Biography = includeOptionalFields ? "Productora local" : null
        };
        _repository.Setup(repository => repository.UpdateUserProfileAsync(42, request))
            .Returns(Task.CompletedTask);

        await CreateService().UpdateUserProfileAsync(42, request);

        Assert.Equal(" Ana ", request.FirstName);
        _repository.Verify(repository => repository.UpdateUserProfileAsync(42,
            It.Is<UpdateUserProfileDto>(dto => ReferenceEquals(dto, request))), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestUpdateUserProfileWithRepositoryFailureShouldPropagateSameError()
    {
        var request = new UpdateUserProfileDto { FirstName = "Ana", PaternalLastName = "López" };
        var expected = new InvalidOperationException(ErrorMessages.DatabaseError);
        _repository.Setup(repository => repository.UpdateUserProfileAsync(42, request)).ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().UpdateUserProfileAsync(42, request));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.UpdateUserProfileAsync(42, request), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestUpdateProfilePictureShouldAssociateExactUrlWithUser()
    {
        const string url = "https://images.example.test/ana.jpg";
        _repository.Setup(repository => repository.UpdateProfilePictureAsync(42, url))
            .Returns(Task.CompletedTask);

        await CreateService().UpdateProfilePictureAsync(42, url);

        _repository.Verify(repository => repository.UpdateProfilePictureAsync(42, url), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestUpdateProfilePictureWithRepositoryFailureShouldPropagateSameError()
    {
        const string url = "https://images.example.test/ana.jpg";
        var expected = new InvalidOperationException(ErrorMessages.DatabaseError);
        _repository.Setup(repository => repository.UpdateProfilePictureAsync(42, url)).ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().UpdateProfilePictureAsync(42, url));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.UpdateProfilePictureAsync(42, url), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestPromoteToProducerShouldForwardUserAndCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();
        _repository.Setup(repository => repository.PromoteToProducerAsync(42, cancellation.Token))
            .Returns(Task.CompletedTask);

        await CreateService().PromoteToProducerAsync(42, cancellation.Token);

        _repository.Verify(repository => repository.PromoteToProducerAsync(42, cancellation.Token), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task TestPromoteToProducerWithInvalidIdShouldRejectBeforeRepository(int userId)
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().PromoteToProducerAsync(userId));

        Assert.Equal(ErrorMessages.RecordNotFound, error.Message);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestPromoteToProducerWithExistingRoleShouldPropagateRepositoryError()
    {
        var expected = new ArgumentException(ErrorMessages.UserAlreadyProducer);
        _repository.Setup(repository => repository.PromoteToProducerAsync(42, default)).ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().PromoteToProducerAsync(42));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.PromoteToProducerAsync(42, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    private UserService CreateService() => new(_repository.Object, _logger.Object);
}
