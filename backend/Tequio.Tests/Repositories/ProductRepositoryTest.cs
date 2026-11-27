using System.Data;
using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Dtos;
using Tequio.Infrastructure.Models;
using Tequio.Infrastructure.Repositories;
using Xunit;

namespace Tequio.Tests.Repositories;

[Collection("DatabaseCollection")]
public class ProductRepositoryTest(DatabaseFixture fixture)
{
    #region CreateBaseProductAsync

    [Fact]
    public async Task TestCreateBaseProductWithImageUrlShouldInsertProductAndReturnNewId()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var repository = new ProductRepository(context);

        var dto = new CreateBaseProductDto
        {
            CategoryId = category.Id,
            Name = "  Café de Altura  ",
            ShortDescription = "  Tostado medio artesanal  ",
            ImageUrl = "https://cdn.tequio.com/products/cafe.jpg",
            MeasurementUnit = "  kg  "
        };

        var newProductId = await repository.CreateBaseProductAsync(producer.Id, dto);

        await using var assertContext = fixture.CreateDbContext();
        var createdProduct = await assertContext.BaseProducts.FindAsync(newProductId);

        Assert.True(newProductId > 0);
        Assert.NotNull(createdProduct);
        var expected = new
        {
            ProducerId = producer.Id,
            dto.CategoryId,
            Name = dto.Name.Trim(),
            ShortDescription = dto.ShortDescription.Trim(),
            ImageUrl = (string?)dto.ImageUrl,
            MeasurementUnit = dto.MeasurementUnit.Trim(),
            IsActive = true
        };
        var actual = new
        {
            createdProduct.ProducerId,
            createdProduct.CategoryId,
            createdProduct.Name,
            createdProduct.ShortDescription,
            createdProduct.ImageUrl,
            createdProduct.MeasurementUnit,
            createdProduct.IsActive
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestCreateBaseProductWithWhitespaceImageUrlShouldStoreNullImageUrl()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var repository = new ProductRepository(context);

        var dto = new CreateBaseProductDto
        {
            CategoryId = category.Id,
            Name = "Miel Multifloral",
            ShortDescription = "Miel cruda sin filtrar",
            ImageUrl = "   ",
            MeasurementUnit = "L"
        };

        var newProductId = await repository.CreateBaseProductAsync(producer.Id, dto);

        await using var assertContext = fixture.CreateDbContext();
        var createdProduct = await assertContext.BaseProducts.FindAsync(newProductId);

        Assert.True(newProductId > 0);
        Assert.NotNull(createdProduct);
        Assert.Null(createdProduct.ImageUrl);
    }

    [Fact]
    public async Task TestCreateBaseProductWhenUserIsNotProducerShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var buyer = await SeedUserAsync(context, roleId: 1);
        var category = await SeedCategoryAsync(context);
        var repository = new ProductRepository(context);

        var dto = new CreateBaseProductDto
        {
            CategoryId = category.Id,
            Name = "Queso Fresco",
            ShortDescription = "Queso artesanal de rancho",
            MeasurementUnit = "kg"
        };

        // Raises ;THROW 50012 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.CreateBaseProductAsync(buyer.Id, dto));
    }

    [Fact]
    public async Task TestCreateBaseProductWhenCategoryDoesNotExistShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var repository = new ProductRepository(context);

        var dto = new CreateBaseProductDto
        {
            CategoryId = -9999,
            Name = "Salsa Macha",
            ShortDescription = "Salsa tradicional de chile seco",
            MeasurementUnit = "Frasco"
        };

        // Raises ;THROW 50013 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.CreateBaseProductAsync(producer.Id, dto));
    }

    [Fact]
    public async Task TestCreateBaseProductWhenActiveProductNameAlreadyExistsShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Café Orgánico", isActive: true);
        var repository = new ProductRepository(context);

        var duplicateDto = new CreateBaseProductDto
        {
            CategoryId = category.Id,
            Name = "  café orgánico  ",
            ShortDescription = "Otra presentación",
            MeasurementUnit = "kg"
        };

        // Raises ;THROW 50014 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.CreateBaseProductAsync(producer.Id, duplicateDto));
    }

    [Fact]
    public async Task TestCreateBaseProductWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var repository = new ProductRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var dto = new CreateBaseProductDto
        {
            CategoryId = category.Id,
            Name = "Producto Cancelado",
            ShortDescription = "Descripción de prueba",
            MeasurementUnit = "Pieza"
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.CreateBaseProductAsync(producer.Id, dto, cts.Token));
    }

    #endregion

    #region DeactivateBaseProductAsync

    [Fact]
    public async Task TestDeactivateBaseProductWhenProductIsActiveAndHasNoBlockingBatchesShouldSetIsActiveFalse()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var product = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Pan Artesanal", isActive: true);
        var repository = new ProductRepository(context);

        await repository.DeactivateBaseProductAsync(product.Id, producer.Id);

        await using var assertContext = fixture.CreateDbContext();
        var updatedProduct = await assertContext.BaseProducts.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.False(updatedProduct.IsActive);
    }

    [Fact]
    public async Task TestDeactivateBaseProductWhenProductDoesNotBelongToProducerShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var owner = await SeedUserAsync(context, roleId: 2);
        var otherProducer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var product = await SeedBaseProductAsync(context, owner.Id, category.Id, name: "Mermelada de Zarzamora", isActive: true);
        var repository = new ProductRepository(context);

        // Raises ;THROW 50015 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.DeactivateBaseProductAsync(product.Id, otherProducer.Id));
    }

    [Fact]
    public async Task TestDeactivateBaseProductWhenProductIsAlreadyDeactivatedShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var inactiveProduct = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Producto Inactivo", isActive: false);
        var repository = new ProductRepository(context);

        // Raises ;THROW 50016 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.DeactivateBaseProductAsync(inactiveProduct.Id, producer.Id));
    }

    [Fact]
    public async Task TestDeactivateBaseProductWhenProductHasActiveBatchShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var product = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Hongo Zeta", isActive: true);
        await SeedActiveBatchAsync(context, product.Id, producer.Id);
        var repository = new ProductRepository(context);

        // Raises ;THROW 50017 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.DeactivateBaseProductAsync(product.Id, producer.Id));
    }

    [Fact]
    public async Task TestDeactivateBaseProductWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);
        var product = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Naranja", isActive: true);
        var repository = new ProductRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.DeactivateBaseProductAsync(product.Id, producer.Id, cts.Token));
    }

    #endregion

    #region GetProductsFromProducerAsync

    [Fact]
    public async Task TestGetProductsFromProducerWhenActiveProductsExistShouldReturnPaginatedActiveProducts()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);

        var first = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Café 1", imageUrl: "https://cdn.tequio.com/p1.jpg", isActive: true);
        var second = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Café 2", imageUrl: null, isActive: true);
        await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Café 3", imageUrl: null, isActive: true);
        await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Café Inactivo", imageUrl: null, isActive: false);

        var repository = new ProductRepository(context);

        var result = await repository.GetProductsFromProducerAsync(producer.Id, pageIndex: 0, pageSize: 2);

        var expectedPage = new { PageIndex = 0, PageSize = 2, ItemCount = 3 };
        var actualPage = new { result.PageIndex, result.PageSize, result.ItemCount };
        Assert.Equal(expectedPage, actualPage);

        var expectedProducts = new[] { first, second }.Select(p => new
        {
            p.Id,
            p.ProducerId,
            p.CategoryId,
            p.Name,
            p.ShortDescription,
            p.ImageUrl,
            p.MeasurementUnit,
            p.IsActive
        }).ToList();

        var actualProducts = result.Products.Select(p => new
        {
            p.Id,
            p.ProducerId,
            p.CategoryId,
            p.Name,
            p.ShortDescription,
            p.ImageUrl,
            p.MeasurementUnit,
            p.IsActive
        }).ToList();

        Assert.Equal(expectedProducts, actualProducts);
    }

    [Fact]
    public async Task TestGetProductsFromProducerWhenProducerHasNoActiveProductsShouldReturnEmptyPage()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var repository = new ProductRepository(context);

        var result = await repository.GetProductsFromProducerAsync(producer.Id, pageIndex: 0, pageSize: 10);

        Assert.Equal(new { PageIndex = 0, PageSize = 10, ItemCount = 0 }, new { result.PageIndex, result.PageSize, result.ItemCount });
        Assert.Empty(result.Products);
    }

    [Theory]
    [InlineData(-1, 10)]  // ;THROW 50006
    [InlineData(0, 0)]    // ;THROW 50007
    [InlineData(0, 101)]  // ;THROW 50008
    public async Task TestGetProductsFromProducerWithInvalidPaginationShouldThrowMappedException(int pageIndex, int pageSize)
    {
        await using var context = fixture.CreateDbContext();
        var repository = new ProductRepository(context);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.GetProductsFromProducerAsync(producerId: 1, pageIndex, pageSize));
    }

    [Fact]
    public async Task TestGetProductsFromProducerWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new ProductRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetProductsFromProducerAsync(producerId: 1, pageIndex: 0, pageSize: 10, cts.Token));
    }

    #endregion

    #region GetProductsByCategoryAsync

    [Fact]
    public async Task TestGetProductsByCategoryWhenActiveProductsExistShouldReturnPaginatedActiveProducts()
    {
        await using var context = fixture.CreateDbContext();
        var producer = await SeedUserAsync(context, roleId: 2);
        var category = await SeedCategoryAsync(context);

        var first = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Miel 1", imageUrl: "https://cdn.tequio.com/m1.jpg", isActive: true);
        var second = await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Miel 2", imageUrl: null, isActive: true);
        await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Miel 3", imageUrl: null, isActive: true);
        await SeedBaseProductAsync(context, producer.Id, category.Id, name: "Miel Inactiva", imageUrl: null, isActive: false);

        var repository = new ProductRepository(context);

        var result = await repository.GetProductsByCategoryAsync(category.Id, pageIndex: 0, pageSize: 2);

        Assert.Equal(new { PageIndex = 0, PageSize = 2, ItemCount = 3 }, new { result.PageIndex, result.PageSize, result.ItemCount });

        var expectedProducts = new[] { first, second }.Select(p => new
        {
            p.Id,
            p.ProducerId,
            p.CategoryId,
            p.Name,
            p.ShortDescription,
            p.ImageUrl,
            p.MeasurementUnit,
            p.IsActive
        }).ToList();

        var actualProducts = result.Products.Select(p => new
        {
            p.Id,
            p.ProducerId,
            p.CategoryId,
            p.Name,
            p.ShortDescription,
            p.ImageUrl,
            p.MeasurementUnit,
            p.IsActive
        }).ToList();

        Assert.Equal(expectedProducts, actualProducts);
    }

    [Fact]
    public async Task TestGetProductsByCategoryWhenCategoryHasNoActiveProductsShouldReturnEmptyPage()
    {
        await using var context = fixture.CreateDbContext();
        var emptyCategory = await SeedCategoryAsync(context);
        var repository = new ProductRepository(context);

        var result = await repository.GetProductsByCategoryAsync(emptyCategory.Id, pageIndex: 0, pageSize: 10);

        Assert.Equal(new { PageIndex = 0, PageSize = 10, ItemCount = 0 }, new { result.PageIndex, result.PageSize, result.ItemCount });
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task TestGetProductsByCategoryWhenConnectionIsAlreadyOpenShouldKeepConnectionOpen()
    {
        await using var context = fixture.CreateDbContext();
        var emptyCategory = await SeedCategoryAsync(context);
        var repository = new ProductRepository(context);

        // Covers branch shouldCloseConnection == false at ExecuteWithConnectionAsync
        await context.Database.OpenConnectionAsync();

        var result = await repository.GetProductsByCategoryAsync(emptyCategory.Id, pageIndex: 0, pageSize: 10);

        Assert.Equal(0, result.ItemCount);
        Assert.Equal(ConnectionState.Open, context.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData(-1, 10)]  // ;THROW 50009
    [InlineData(0, 0)]    // ;THROW 50010
    [InlineData(0, 101)]  // ;THROW 50011
    public async Task TestGetProductsByCategoryWithInvalidPaginationShouldThrowMappedException(int pageIndex, int pageSize)
    {
        await using var context = fixture.CreateDbContext();
        var repository = new ProductRepository(context);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.GetProductsByCategoryAsync(categoryId: 1, pageIndex, pageSize));
    }

    [Fact]
    public async Task TestGetProductsByCategoryWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new ProductRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetProductsByCategoryAsync(categoryId: 1, pageIndex: 0, pageSize: 10, cts.Token));
    }

    #endregion

    #region Helpers

    private static async Task<User> SeedUserAsync(TequioDbContext context, int roleId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var user = new User
        {
            RoleId = roleId,
            Email = $"prod_{uniqueId}@tequio.com",
            PasswordHash = "hashed_password_placeholder",
            FirstName = "Productor",
            PaternalLastName = "Prueba",
            BirthDate = new DateOnly(1990, 4, 15),
            IsVerified = true,
            RegistrationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<ProductCategory> SeedCategoryAsync(TequioDbContext context)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var category = new ProductCategory
        {
            Name = $"Categoría Test {uniqueId}"
        };

        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task<BaseProduct> SeedBaseProductAsync(
        TequioDbContext context,
        int producerId,
        int categoryId,
        string name,
        string? imageUrl = "https://cdn.tequio.com/default.jpg",
        bool isActive = true)
    {
        var product = new BaseProduct
        {
            ProducerId = producerId,
            CategoryId = categoryId,
            Name = name,
            ShortDescription = "Descripción corta de prueba",
            ImageUrl = imageUrl,
            MeasurementUnit = "kg",
            IsActive = isActive
        };

        context.BaseProducts.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static async Task SeedActiveBatchAsync(TequioDbContext context, int productId, int producerId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var deliveryPoint = new DeliveryPoint
        {
            Name = $"Punto Test {uniqueId}",
            Street = "Calle 1",
            Number = "10",
            City = "Xalapa",
            MunicipalityId = 1,
            ZipCode = "91000",
            Neighborhood = "Centro",
            BusinessHours = "09:00-18:00",
            Latitude = 19.54377000m,
            Longitude = -96.91018000m,
            IsActive = true
        };

        context.DeliveryPoints.Add(deliveryPoint);
        await context.SaveChangesAsync();

        var batch = new Batch
        {
            BaseProductId = productId,
            ProducerId = producerId,
            DeliveryPointId = deliveryPoint.Id,
            BatchName = $"Lote Activo {uniqueId}",
            Description = "Descripción de lote activo",
            MinimumGoal = 10,
            Deadline = DateTime.UtcNow.AddDays(7),
            Status = "Active",
            PublicationDate = DateTime.UtcNow
        };

        context.Batches.Add(batch);
        await context.SaveChangesAsync();
    }

    #endregion
}