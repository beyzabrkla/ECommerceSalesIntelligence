using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Mappings;
using ECommerceSalesIntelligence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(cfg => { },
    typeof(GeneralMapping));

builder.Services.AddSingleton<MLContext>();
builder.Services.AddScoped<ForecastingService>();
builder.Services.AddScoped<ClassificationService>();
builder.Services.AddScoped<MulticlassClassificationService>();
builder.Services.AddScoped<AnomalyDetectionService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();