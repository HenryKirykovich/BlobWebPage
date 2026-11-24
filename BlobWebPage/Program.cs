var builder = WebApplication.CreateBuilder(args);

// Enable MVC (controllers + views)
builder.Services.AddControllersWithViews();

// Create ONE Azure Blob client for the whole app (singleton)
// and give it to the controllers automatically (dependency injection).
builder.Services.AddSingleton(x =>
{
    // The connection string is NOT stored in the code or in appsettings.json.
    //  - Local run:  User Secrets  -> BlobStorage:ConnectionString
    //  - Azure:      App Service app setting named  BlobStorage__ConnectionString
    var connectionString = builder.Configuration["BlobStorage:ConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Missing setting 'BlobStorage:ConnectionString'. " +
            "Set it with User Secrets (local) or an App Service app setting " +
            "named 'BlobStorage__ConnectionString' (Azure).");
    }

    return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Home page of the site = Blob/Index (the resource library)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Blob}/{action=Index}/{id?}");

app.Run();
