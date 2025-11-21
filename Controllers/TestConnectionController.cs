using Microsoft.AspNetCore.Mvc;
using SaborVeloz.Data;

namespace SaborVeloz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestConnectionController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TestConnectionController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Test()
        {
            try
            {
                var count = _db.Productos.Count();
                return Ok($"✅ Conectado correctamente a la base de datos. Productos actuales: {count}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error de conexión: {ex.Message}");
            }
        }
    }
}
