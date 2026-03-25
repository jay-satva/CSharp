using FinalExam.Data;
using FinalExam.Services;
using FinalExam.Middlewares;
using FinalExam.Settings;
//using QuickBooksServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

//var obj = new Class1();

// Bind MongoDbSettings for repositories using IOptions<MongoDbSettings>
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// Session — must be registered before app.Build()
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // SameSite=None required if frontend and backend are on different ports
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// DI
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<QuickBooksService>();

// CORS — AllowCredentials() is required for session cookies to work cross-origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:7142", "http://localhost:5130")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // needed for session cookie
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseStaticFiles();

// Order matters — CORS and Session must come before routing/auth
app.UseCors("AllowReact");
app.UseSession();          // must be before UseRouting so session is available in controllers
app.UseRouting();

// Custom middleware for token auto-refresh
app.UseMiddleware<RefreshMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
