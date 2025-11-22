using Microsoft.EntityFrameworkCore;
using SaborVeloz.Data;
using System.Text.Json.Serialization; // <--- OJO: Necesario para evitar errores de JSON

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar JSON para que ignore ciclos (evita que la cocina explote)
builder.Services.AddControllers().AddJsonOptions(x =>
   x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Conexión a Base de Datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSaborVeloz")));

// 3. Configurar Autenticación (ESTO ES LO QUE FALLABA)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        // ANTES: "/api/auth/login" (Error 404) -> AHORA: "/index.html" (Correcto)
        options.LoginPath = "/index.html";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. Activar Archivos Estáticos (Para que se vea el Frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();