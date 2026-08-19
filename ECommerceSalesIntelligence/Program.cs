using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Mappings;
using ECommerceSalesIntelligence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

var builder = WebApplication.CreateBuilder(args);

// SQL Server baðlantýsý aktif
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS ayarý (Frontend farklý porttaysa isteklerin engellenmemesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddAutoMapper(cfg => { }, typeof(GeneralMapping));

builder.Services.AddSingleton<MLContext>();
builder.Services.AddScoped<ForecastingService>();
builder.Services.AddScoped<BinaryClassificationService>();
builder.Services.AddScoped<MulticlassClassificationService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<ClusteringService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// CORS middleware'i buraya eklenmeli
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();