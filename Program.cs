using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

// 1. Servicios para Controladores (API)
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
// 2. Swagger (Documentación de API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Conexión a Base de Datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSaborVeloz")));

// 4. Autenticación (Cookies)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        options.LoginPath = "/index.html"; // Si no está logueado, lo manda al Login del frontend
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // La sesión dura 1 hora
    });

// 5. Autorización
builder.Services.AddAuthorization();

var app = builder.Build();

// --- CONFIGURACIÓN DEL PIPELINE (El orden importa mucho) ---

// A. Swagger en desarrollo

    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

// B. ?? ACTIVAR EL FRONTEND (WWWROOT) ??
// Esto permite que index.html cargue al entrar a la raíz
app.UseDefaultFiles();
// Esto permite servir los archivos css, js e imagenes de wwwroot
app.UseStaticFiles();

// C. Seguridad (Auth)
app.UseAuthentication();
app.UseAuthorization();

// D. Mapeo de Controladores
app.MapControllers();

app.Run();