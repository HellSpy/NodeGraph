using NodeGraphWeb.Services;
using NodeGraphWeb.Services2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // For MVC
builder.Services.AddScoped<GraphService>(); // register GraphService
builder.Services.AddTransient<GraphServiceBuild>(); // register the build graph service

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();


app.MapRazorPages();
app.MapControllers(); // This is crucial for routing API controllers

app.Run();
