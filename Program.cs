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

var app = builder.Build();

// Swagger visible en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ? QUITAMOS Authentication
// ? QUITAMOS Authorization
// ? QUITAMOS RolMiddleware
// ? QUITAMOS seguridad de Swagger

app.MapControllers();

app.Run();
