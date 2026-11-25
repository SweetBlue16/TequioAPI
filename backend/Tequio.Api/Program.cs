using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tequio.Api.Middlewares;
using Tequio.Domain.Email;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Tequio.Domain.VerificationCodes;
using Tequio.Infrastructure.Data;
using Tequio.Infrastructure.Email;
using Tequio.Infrastructure.Models;
using Tequio.Infrastructure.Repositories;
using Tequio.Infrastructure.Security;
using Tequio.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

string[] allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("TequioCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddControllers();
builder.Services.PostConfigure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    var defaultResponse = options.InvalidModelStateResponseFactory;
    options.InvalidModelStateResponseFactory = context =>
    {
        if (context.ActionDescriptor.RouteValues["controller"] != "DeliveryPoint")
        {
            return defaultResponse(context);
        }
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            error = "Verifica los campos obligatorios, sus longitudes y las coordenadas.",
            errors = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(entry => entry.Key, _ => ApiValidationMessages.InvalidValue)
        });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.UseStartTls, "Email:UseStartTls must be enabled.");

builder.Services.AddDbContext<TequioDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TequioDatabase"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

builder.Services.AddSingleton(_ =>
{
    var key = Environment.GetEnvironmentVariable("TEQUIO_JWT_SIGNING_KEY");

    if (string.IsNullOrWhiteSpace(key))
    {
        key = builder.Configuration["Jwt:Key"];
    }

    if (string.IsNullOrWhiteSpace(key) && builder.Environment.IsDevelopment())
    {
        key = new ConfigurationBuilder().AddUserSecrets<Program>(optional: true).Build()["Jwt:Key"];
    }

    if (string.IsNullOrWhiteSpace(key))
    {
        throw new InvalidOperationException(
            "Set TEQUIO_JWT_SIGNING_KEY or configure Jwt:Key in development user secrets.");
    }

    return new JwtSigningKey(Encoding.UTF8.GetBytes(key));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Reject older Admin tokens issued before pending-account checks were enforced.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.IsInRole("Admin") == true &&
                    context.Principal.FindFirst("admin_authorized")?.Value != "true")
                {
                    context.Fail("La cuenta requiere iniciar sesión nuevamente tras su autorización.");
                }
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtSigningKey>((options, key) => options.TokenValidationParameters.IssuerSigningKey = key.Key);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IDeliveryPointRepository, DeliveryPointRepository>();
builder.Services.AddScoped<IDeliveryPointService, DeliveryPointService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddSingleton<IVerificationCodeGenerator, VerificationCodeGenerator>();
builder.Services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
builder.Services.AddSingleton<ISmtpClientFactory, SmtpClientFactory>();
builder.Services.AddTransient<IEmailService, EmailService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(
        new { error = "Ocurrió un error inesperado." },
        cancellationToken: context.RequestAborted);
}));
app.UseMiddleware<GlobalExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAdminAsync();
}

bool isSwaggerEnabled = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Swagger:Enabled");

if (isSwaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("TequioCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    string[]? arrayOptions = configuration.GetSection("AllowedOrigins").Get<string[]>();

    if (arrayOptions is { Length: > 0 })
    {
        return arrayOptions
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .ToArray();
    }

    string? rawOrigins = configuration["AllowedOrigins"];

    if (string.IsNullOrWhiteSpace(rawOrigins))
    {
        return Array.Empty<string>();
    }

    return rawOrigins
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray();
}

internal static class ApiValidationMessages
{
    internal static readonly string[] InvalidValue = ["El valor es obligatorio o no es válido."];
}
