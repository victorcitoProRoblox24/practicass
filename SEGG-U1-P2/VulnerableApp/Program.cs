using Microsoft.EntityFrameworkCore;
using VulnerableApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1. LOS SERVICIOS VAN AQUÍ (Antes del Build)
builder.Services.AddSession(); 

// --- LÍNEA DE CORTE ---
var app = builder.Build(); 
// A partir de aquí ya no puedes usar builder.Services

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 2. EL MIDDLEWARE VA AQUÍ
app.UseSession(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();