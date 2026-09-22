using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
// OJO: no hace falta "using QuestPDF.Infrastructure;" aquí — el propio
// paquete QuestPDF ya lo agrega solo (por eso .NET avisaba "duplicado"
// cuando lo escribíamos a mano).

// QuestPDF exige elegir un tipo de licencia antes de generar el primer PDF.
// "Community" es la licencia gratuita (para proyectos/empresas pequeñas,
// como este). Se registra una sola vez, al arrancar la aplicación.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF (generador de los reportes en PDF, ver Services/ReportesPdfService.cs)
// necesita que se elija un tipo de licencia ANTES de generar cualquier PDF.
// "Community" es gratuita: individuos, sin fines de lucro y empresas con
// menos de 1 millón USD de ingresos anuales al año. Más info:
// https://www.questpdf.com/license/
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        options.Scope.Add("user:email");
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<EcommerceApp.Services.SupabaseStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// IMPORTANTE PARA RENDER (o cualquier hosting detrás de un proxy/balanceador):
// Render recibe la visita por HTTPS, pero puertas adentro le entrega la
// petición a la aplicación como si fuera HTTP. Sin esto, ASP.NET Core cree
// que TODO el sitio es http, y arma las URLs de vuelta ("redirect_uri") de
// Google/GitHub como "http://...". Como en el panel de Google/GitHub está
// registrado "https://...", no coinciden y el login rechaza el acceso.
// Esto va ANTES de UseHttpsRedirection() y de UseAuthentication(): corrige el
// esquema (http/https) que ve el resto de la aplicación desde el principio.
//
// KnownNetworks y KnownProxies se vacían porque, por seguridad, ASP.NET Core
// por defecto solo confía en el proxy si conoce su IP exacta. La IP interna
// de Render no es fija, así que se le pide confiar en el encabezado
// "X-Forwarded-*" que manda cualquier proxy delante de la aplicación (algo
// seguro aquí, porque Render es el único que puede hablarle a la app).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// "KnownNetworks" quedó obsoleta a partir de .NET 10 (Microsoft avisa con
// ASPDEPR005); "KnownIPNetworks" es su reemplazo oficial.
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// RUTA POR DEFECTO: al entrar a la raíz del sitio (adoptapatitas.onrender.com/)
// se muestra directamente el CATÁLOGO de mascotas (Mascotas/Index), sin pedir
// iniciar sesión. El login solo se le pide a la persona cuando quiere ADOPTAR.
// (Antes era Account/Login, por eso el sitio abría en la pantalla de login.)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Mascotas}/{action=Index}/{id?}");

// Crear la base de datos y las tablas automáticamente si no existen (sin migraciones)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Crear roles por defecto y un usuario de Zoonosis (administración) inicial si no existen
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Adoptante", "Zoonosis", "Veterinario", "Admin" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Crear un usuario de Zoonosis inicial para pruebas (configurable vía appsettings)
    var adminEmail = app.Configuration["Admin:Email"] ?? "admin@localhost";
    var adminPassword = app.Configuration["Admin:Password"] ?? "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nombre = "Zoonosis",
            Apellido = "Cochabamba",
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Zoonosis");
        }
    }
    else
    {
        if (!await userManager.IsInRoleAsync(adminUser, "Zoonosis"))
            await userManager.AddToRoleAsync(adminUser, "Zoonosis");
    }
}

app.Run();