using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger (sin seguridad)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Conexión a base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSaborVeloz")));
// ? LÍNEA CLAVE 1: Configura el esquema de Autenticación por Cookies (CookieAuth)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        options.LoginPath = "/api/auth/login";
    });

// ? LÍNEA CLAVE 2: Configura el servicio de Autorización
builder.Services.AddAuthorization();
var app = builder.Build();

// Swagger visible en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
