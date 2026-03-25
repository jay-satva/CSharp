using OAuthDemo.Data;
using OAuthDemo.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5141");
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<TokenRepository>();
builder.Services.AddScoped<SqlRepository>();
builder.Services.AddScoped<QuickBooksService>();

var app = builder.Build();

// app.UseHttpsRedirection();

app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();