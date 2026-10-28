using Microsoft.Extensions.Logging;
using Moq;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Xunit;

namespace Tequio.Tests.Products;

/// <summary>Tests catalog business rules and repository orchestration in isolation.</summary>
public sealed class ProductServiceTest
{
    private readonly Mock<IProductRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ProductService>> _logger = new();

    [Theory]
    [InlineData("  https://images.example.test/maiz.jpg  ", "https://images.example.test/maiz.jpg")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t ", null)]
    public async Task TestCreateBaseProductShouldNormalizeBeforePersistenceAndReturnId(
        string? imageUrl, string? expectedImageUrl)
    {
        var request = CreateRequest();
        request.Name = "  Maíz \t";
        request.ShortDescription = "  Maíz de cultivo local  ";
        request.MeasurementUnit = " kg ";
        request.ImageUrl = imageUrl;
        _repository.Setup(repository => repository.CreateBaseProductAsync(42,
                It.Is<CreateBaseProductDto>(dto => ReferenceEquals(dto, request) &&
                    dto.CategoryId == 3 && dto.Name == "Maíz" &&
                    dto.ShortDescription == "Maíz de cultivo local" &&
                    dto.MeasurementUnit == "kg" && dto.ImageUrl == expectedImageUrl), default))
            .ReturnsAsync(71);

        int result = await CreateService().CreateBaseProductAsync(42, request);

        Assert.Equal(71, result);
        _repository.Verify(repository => repository.CreateBaseProductAsync(42, request, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task TestCreateBaseProductWithMissingTextShouldNormalizeAndDelegate(string? value)
    {
        // DTO annotations run in the HTTP layer; this service only trims these fields.
        var request = CreateRequest();
        request.Name = value!;
        request.ShortDescription = value!;
        request.MeasurementUnit = value!;
        _repository.Setup(repository => repository.CreateBaseProductAsync(42,
                It.Is<CreateBaseProductDto>(dto => dto.Name == string.Empty &&
                    dto.ShortDescription == string.Empty && dto.MeasurementUnit == string.Empty), default))
            .ReturnsAsync(71);

        int result = await CreateService().CreateBaseProductAsync(42, request);

        Assert.Equal(71, result);
        _repository.Verify(repository => repository.CreateBaseProductAsync(42, request, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 3, ErrorMessages.ProducerNotFoundOrUnauthorized)]
    [InlineData(-1, 3, ErrorMessages.ProducerNotFoundOrUnauthorized)]
    [InlineData(42, 0, ErrorMessages.CategoryNotFound)]
    [InlineData(42, -1, ErrorMessages.CategoryNotFound)]
    public async Task TestCreateBaseProductWithInvalidIdShouldRejectBeforeRepository(
        int producerId, int categoryId, string expectedMessage)
    {
        var request = CreateRequest();
        request.CategoryId = categoryId;

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBaseProductAsync(producerId, request));

        Assert.Equal(expectedMessage, error.Message);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ErrorMessages.DuplicateProductName)]
    [InlineData(ErrorMessages.CategoryNotFound)]
    [InlineData(ErrorMessages.ProducerNotFoundOrUnauthorized)]
    public async Task TestCreateBaseProductWithRepositoryRejectionShouldPropagateSameError(string message)
    {
        var request = CreateRequest();
        var expected = new ArgumentException(message);
        _repository.Setup(repository => repository.CreateBaseProductAsync(42, request, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBaseProductAsync(42, request));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.CreateBaseProductAsync(42, request, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestCreateBaseProductWithPersistenceFailureShouldPropagateSameError()
    {
        var request = CreateRequest();
        var expected = new InvalidOperationException(ErrorMessages.DatabaseError);
        _repository.Setup(repository => repository.CreateBaseProductAsync(42, request, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().CreateBaseProductAsync(42, request));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.CreateBaseProductAsync(42, request, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDeactivateBaseProductShouldDelegateProductAndProducerOnce()
    {
        _repository.Setup(repository => repository.DeactivateBaseProductAsync(71, 42, default))
            .Returns(Task.CompletedTask);

        await CreateService().DeactivateBaseProductAsync(71, 42);

        _repository.Verify(repository => repository.DeactivateBaseProductAsync(71, 42, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 42, ErrorMessages.RecordNotFound)]
    [InlineData(-1, 42, ErrorMessages.RecordNotFound)]
    [InlineData(71, 0, ErrorMessages.ProducerNotFoundOrUnauthorized)]
    [InlineData(71, -1, ErrorMessages.ProducerNotFoundOrUnauthorized)]
    public async Task TestDeactivateBaseProductWithInvalidIdShouldRejectBeforeRepository(
        int productId, int producerId, string expectedMessage)
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().DeactivateBaseProductAsync(productId, producerId));

        Assert.Equal(expectedMessage, error.Message);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ErrorMessages.ProductAlreadyDeactivated)]
    [InlineData(ErrorMessages.ProductHasActiveBatches)]
    [InlineData(ErrorMessages.DatabaseError)]
    public async Task TestDeactivateBaseProductWithRepositoryFailureShouldPropagateSameError(string message)
    {
        var expected = new InvalidOperationException(message);
        _repository.Setup(repository => repository.DeactivateBaseProductAsync(71, 42, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().DeactivateBaseProductAsync(71, 42));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.DeactivateBaseProductAsync(71, 42, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDeactivateBaseProductWithMissingOrForeignProductShouldPropagateRepositoryError()
    {
        var expected = new ArgumentException(ErrorMessages.ProductNotFoundOrForbidden);
        _repository.Setup(repository => repository.DeactivateBaseProductAsync(71, 42, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().DeactivateBaseProductAsync(71, 42));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.DeactivateBaseProductAsync(71, 42, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(2, 25, true)]
    public async Task TestGetProductsFromProducerShouldForwardPaginationAndReturnSamePage(
        int pageIndex, int pageSize, bool empty)
    {
        var page = CreatePage(pageIndex, pageSize, empty);
        _repository.Setup(repository => repository.GetProductsFromProducerAsync(42, pageIndex, pageSize, default))
            .ReturnsAsync(page);

        var result = await CreateService().GetProductsFromProducer(42, pageIndex, pageSize);

        Assert.Same(page, result);
        _repository.Verify(repository => repository.GetProductsFromProducerAsync(42, pageIndex, pageSize, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(2, 25, true)]
    public async Task TestGetProductsByCategoryShouldForwardPaginationAndReturnSamePage(
        int pageIndex, int pageSize, bool empty)
    {
        var page = CreatePage(pageIndex, pageSize, empty);
        _repository.Setup(repository => repository.GetProductsByCategoryAsync(3, pageIndex, pageSize, default))
            .ReturnsAsync(page);

        var result = await CreateService().GetProductsByCategory(3, pageIndex, pageSize);

        Assert.Same(page, result);
        _repository.Verify(repository => repository.GetProductsByCategoryAsync(3, pageIndex, pageSize, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestGetProductsFromProducerWithRepositoryFailureShouldPropagateSameError()
    {
        var expected = new InvalidOperationException(ErrorMessages.DatabaseError);
        _repository.Setup(repository => repository.GetProductsFromProducerAsync(42, 0, 10, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetProductsFromProducer(42, 0, 10));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.GetProductsFromProducerAsync(42, 0, 10, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestGetProductsByCategoryWithRepositoryFailureShouldPropagateSameError()
    {
        var expected = new InvalidOperationException(ErrorMessages.DatabaseError);
        _repository.Setup(repository => repository.GetProductsByCategoryAsync(3, 0, 10, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetProductsByCategory(3, 0, 10));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.GetProductsByCategoryAsync(3, 0, 10, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1, 10, ErrorMessages.NegativePageIndex)]
    [InlineData(0, 0, ErrorMessages.InvalidPageSize)]
    [InlineData(0, 101, ErrorMessages.PageSizeExceeded)]
    public async Task TestGetProductsFromProducerWithInvalidPaginationShouldDelegateAndPropagateRepositoryValidation(
        int pageIndex, int pageSize, string message)
    {
        var expected = new ArgumentException(message);
        _repository.Setup(repository => repository.GetProductsFromProducerAsync(42, pageIndex, pageSize, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().GetProductsFromProducer(42, pageIndex, pageSize));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.GetProductsFromProducerAsync(42, pageIndex, pageSize, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1, 10, ErrorMessages.NegativePageIndex)]
    [InlineData(0, 0, ErrorMessages.InvalidPageSize)]
    [InlineData(0, 101, ErrorMessages.PageSizeExceeded)]
    public async Task TestGetProductsByCategoryWithInvalidPaginationShouldDelegateAndPropagateRepositoryValidation(
        int pageIndex, int pageSize, string message)
    {
        var expected = new ArgumentException(message);
        _repository.Setup(repository => repository.GetProductsByCategoryAsync(3, pageIndex, pageSize, default))
            .ThrowsAsync(expected);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().GetProductsByCategory(3, pageIndex, pageSize));

        Assert.Same(expected, error);
        _repository.Verify(repository => repository.GetProductsByCategoryAsync(3, pageIndex, pageSize, default), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    private ProductService CreateService() => new(_repository.Object, _logger.Object);

    private static CreateBaseProductDto CreateRequest() => new()
    {
        CategoryId = 3,
        Name = "Maíz",
        ShortDescription = "Maíz de cultivo local",
        MeasurementUnit = "kg"
    };

    private static ProductPageDto CreatePage(int pageIndex, int pageSize, bool empty) => new()
    {
        PageIndex = pageIndex,
        PageSize = pageSize,
        ItemCount = empty ? 0 : 17,
        Products = empty ? [] : new[]
        {
            new ProductDto
            {
                Id = 71, ProducerId = 42, CategoryId = 3, Name = "Maíz",
                ShortDescription = "Maíz de cultivo local", MeasurementUnit = "kg", IsActive = true
            }
        }
    };
}
