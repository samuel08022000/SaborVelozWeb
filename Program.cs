using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using System.Text.Json.Serialization;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar JSON para evitar ciclos
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================
// 2. CONEXIÓN A BASE DE DATOS (BLINDADA) 🛡️
// =========================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// PARCHE: Detectar y corregir formato de Railway si es necesario
if (!string.IsNullOrEmpty(connectionString))
{
    connectionString = connectionString.Trim().Trim('"'); // Quita comillas extra
    try
    {
        if (connectionString.StartsWith("postgres://"))
        {
            // Convierte URL de Railway a ConnectionString de C#
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var builderUrl = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = uri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Prefer
            };
            connectionString = builderUrl.ToString();
        }
    }
    catch { /* Si falla, usa la original */ }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// =========================================================
// 3. CONFIGURAR CORS (EL ARREGLO CLAVE) 🔑
// =========================================================
var NuevaPolitica = "NuevaPolitica";

builder.Services.AddCors(options =>
{
    options.AddPolicy(NuevaPolitica, app =>
    {
        app.WithOrigins(
                "http://localhost:5173",                      // Tu PC
                "https://sabor-veloz-frontend-production.up.railway.app" // 👈 ¡TU FRONTEND EN RAILWAY!
            )
           .AllowAnyHeader()
           .AllowAnyMethod()
           .AllowCredentials(); // ✅ PERMITE QUE PASE LA COOKIE
    });
});

// =========================================================
// 4. AUTENTICACIÓN (COOKIES SIN REDIRECCIÓN) 🍪
// =========================================================
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        options.Cookie.SameSite = SameSiteMode.None; // ✅ OBLIGATORIO PARA RAILWAY
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ✅ OBLIGATORIO HTTPS
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

        // 👇 ESTO EVITA EL ERROR HTML EN EL FRONTEND 👇
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

builder.Services.AddAuthorization();

var app = builder.Build();

// Configurar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// =========================================================
// 5. ORDEN DE TUBERÍAS (CRUCIAL) ⚠️
// =========================================================
app.UseCors(NuevaPolitica); // 👈 SIEMPRE ANTES DE AUTH
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();