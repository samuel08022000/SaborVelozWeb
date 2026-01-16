using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using System.Text.Json.Serialization;
using Npgsql; // Necesario para el parche de conexión

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar JSON (Evitar ciclos infinitos)
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================================
// 2. CONEXIÓN A BASE DE DATOS (BLINDADA PARA RAILWAY) 🛡️
// =========================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// PARCHE: Detectar y corregir formato de Railway si es necesario
if (!string.IsNullOrEmpty(connectionString))
{
    // Si viene con comillas extra al principio o final, las quitamos
    connectionString = connectionString.Trim().Trim('"');

    // Si viene en formato URL (postgres://), lo convertimos a formato estándar
    try
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && uri.Scheme == "postgres")
        {
            var userInfo = uri.UserInfo.Split(':');
            var builderUrl = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = uri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Prefer // Railway suele requerir esto
            };
            connectionString = builderUrl.ToString();
        }
    }
    catch
    {
        // Si falla la conversión, usamos la cadena original confiando en que esté bien
        Console.WriteLine("Advertencia: No se pudo parsear la URL de conexión, se usará la original.");
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// =========================================================================

// 3. Configurar Autenticación (Cookies)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// 4. CONFIGURAR CORS
var NuevaPolitica = "NuevaPolitica";

builder.Services.AddCors(options =>
{
    options.AddPolicy(NuevaPolitica, app =>
    {
        app.WithOrigins(
            "http://localhost:5173", // Para probar en tu PC
            "https://sabor-veloz-frontend-production.up.railway.app" // 👈 ¡PON AQUÍ TU URL REAL DE RAILWAY!
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // ✅ ESTO ES LO QUE HABILITA EL LOGIN
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configurar Swagger (Activado para Desarrollo y si activas la variable en Railway)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 5. ACTIVAR CORS (Siempre antes de Auth)
app.UseCors(NuevaPolitica);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();