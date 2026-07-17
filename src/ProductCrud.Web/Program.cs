using ProductCrud.Web.Data;
using ProductCrud.Web.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found. "
        + "Set the ConnectionStrings__DefaultConnection environment variable, "
        + "or use 'dotnet user-secrets' for local development.");

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISqlConnectionFactory>(
    new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();
