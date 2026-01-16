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
//builder.Services.AddDbContext<AppDbContext>(options =>
//  options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSaborVeloz")));
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
// 3. Configurar Autenticación (Cookies)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "SaborVelozCookie";
        // ELIMINA O COMENTA ESTA LÍNEA:
        // options.LoginPath = "/index.html"; 

        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // AGREGA ESTO: Controlar la redirección para APIs
        options.Events.OnRedirectToLogin = context =>
        {
            // En vez de redirigir, devolvemos error 401
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        // Opcional: Lo mismo para "Prohibido" (403)
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// 4. ?? CONFIGURAR CORS (PARA REACT) ??
var NuevaPolitica = "NuevaPolitica";

builder.Services.AddCors(options =>
{
    options.AddPolicy("NuevaPolitica", app =>
    {
        app.AllowAnyOrigin()  // ✅ Permite que cualquiera (incluido tu frontend de Railway) se conecte
           .AllowAnyHeader()
           .AllowAnyMethod();
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
app.UseStaticFiles();
// 5. ?? ACTIVAR CORS (ANTES DE AUTH Y STATIC FILES) ??
app.UseCors(NuevaPolitica);


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();