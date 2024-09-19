using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibroAPI.Models;
using StackExchange.Redis;

namespace LibroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibrosController : ControllerBase
    {
        private readonly LibroContext _context;
        private readonly IConnectionMultiplexer _redis;

        public LibrosController(LibroContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis;
        }

        // GET: api/Libros
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Libro>>> GetLibroSet()
        {
            var db = _redis.GetDatabase();
            string cacheKey = "libroList";
            var librosCache = await db.StringGetAsync(cacheKey);

            if (!librosCache.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<List<Libro>>(librosCache);
            }

            var libros = await _context.LibroSet.ToListAsync();
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(libros), TimeSpan.FromMinutes(10));

            return libros;
        }

        // GET: api/Libros/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Libro>> GetLibro(int id)
        {
            var db = _redis.GetDatabase();
            string cacheKey = $"libro_{id}";
            var libroCache = await db.StringGetAsync(cacheKey);

            if (!libroCache.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<Libro>(libroCache);
            }

            var libro = await _context.LibroSet.FindAsync(id);

            if (libro == null)
            {
                return NotFound();
            }

            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(libro), TimeSpan.FromMinutes(10));
            return libro;
        }

        // PUT: api/Libros/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLibro(int id, Libro libro)
        {
            if (id != libro.Id)
            {
                return BadRequest();
            }

            _context.Entry(libro).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                var db = _redis.GetDatabase();
                string cacheKeyLibro = $"libro_{id}";
                string cacheKeyList = "libroList";

                await db.KeyDeleteAsync(cacheKeyLibro);
                await db.KeyDeleteAsync(cacheKeyList);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LibroExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Libros
        [HttpPost]
        public async Task<ActionResult<Libro>> PostLibro(Libro libro)
        {
            _context.LibroSet.Add(libro);
            await _context.SaveChangesAsync();

            var db = _redis.GetDatabase();
            string cacheKeyList = "libroList";
            await db.KeyDeleteAsync(cacheKeyList);

            return CreatedAtAction("GetLibro", new { id = libro.Id }, libro);
        }

        // DELETE: api/Libros/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLibro(int id)
        {
            var libro = await _context.LibroSet.FindAsync(id);
            if (libro == null)
            {
                return NotFound();
            }

            _context.LibroSet.Remove(libro);
            await _context.SaveChangesAsync();

            var db = _redis.GetDatabase();
            string cacheKeyLibro = $"libro_{id}";
            string cacheKeyList = "libroList";

            await db.KeyDeleteAsync(cacheKeyLibro);
            await db.KeyDeleteAsync(cacheKeyList);

            return NoContent();
        }

        private bool LibroExists(int id)
        {
            return _context.LibroSet.Any(e => e.Id == id);
        }
    }
}
