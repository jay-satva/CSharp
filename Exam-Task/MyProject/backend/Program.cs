using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyProject.API.Filters;
using MyProject.API.Middlewares;
using MyProject.Application.Interfaces;
using MyProject.Application.Mappings;
using MyProject.Application.Services;
using MyProject.Application.Validators;
using MyProject.Infrastructure.Data;
using MyProject.Infrastructure.Repositories;
using MyProject.Infrastructure.Repositories.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddScoped<AutoValidationFilter>();
    builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<AutoValidationFilter>();
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

    builder.Services.AddSingleton<MongoDbContext>();

    builder.Services.AddHttpClient();

    builder.Services.AddAutoMapper(typeof(MappingProfile));

    builder.Services.AddValidatorsFromAssemblyContaining<SignUpValidator>();

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
    builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IEncryptionService, EncryptionService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IQuickBooksService, QuickBooksService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IItemService, ItemService>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
    builder.Services.AddScoped<IUserService, UserService>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var payload = JsonSerializer.Serialize(new
                    {
                        statusCode = StatusCodes.Status401Unauthorized,
                        errorCode = "UNAUTHORIZED",
                        message = "Authentication is required to access this resource.",
                        traceId = context.HttpContext.TraceIdentifier
                    });

                    await context.Response.WriteAsync(payload);
                },
                OnForbidden = async context =>
                {
                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var payload = JsonSerializer.Serialize(new
                    {
                        statusCode = StatusCodes.Status403Forbidden,
                        errorCode = "FORBIDDEN",
                        message = "You do not have permission to access this resource.",
                        traceId = context.HttpContext.TraceIdentifier
                    });

                    await context.Response.WriteAsync(payload);
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ApiUser", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(ClaimTypes.NameIdentifier);
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(builder.Configuration["Frontend:Url"]!)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasMigrations = db.Database.GetMigrations().Any();
            if (hasMigrations)
            {
                db.Database.Migrate();
            }
            else
            {
                db.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Database migration failed at startup.");
            if (app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Critical startup failure: {ex.Message}");
    throw;
}
