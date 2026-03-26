using System.Security.Claims;
using System.Text;
using FinalExam.Data;
using FinalExam.Middlewares;
using FinalExam.Models;
using FinalExam.Services;
using FinalExam.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<QuickBooksService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<InvoiceRepository>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<JwtService>();

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
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Url"]!, "http://localhost:5130", "https://localhost:7142")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseStaticFiles();
app.UseCors("AllowReact");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RefreshMiddleware>();

app.MapControllers();

app.Run();
