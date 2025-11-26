using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar JSON (Evitar ciclos infinitos)
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Conexión a Base de Datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSaborVeloz")));

// 3. Configurar Autenticación (Cookies)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        options.LoginPath = "/index.html";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

        // ?? IMPORTANTE PARA CORS + COOKIES:
        // Esto permite que la cookie viaje entre el Frontend (React) y el Backend
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Requiere HTTPS (en local usa el certificado de dev)
    });

// 4. ?? CONFIGURAR CORS (PARA REACT) ??
var misOrigenesPermitidos = "_misOrigenesPermitidos";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: misOrigenesPermitidos,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Puertos típicos de React/Vite
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials(); // ?? VITAL: Permite enviar cookies de sesión
                      });
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

// 5. ?? ACTIVAR CORS (ANTES DE AUTH Y STATIC FILES) ??
app.UseCors(misOrigenesPermitidos);

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();