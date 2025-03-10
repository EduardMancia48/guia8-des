﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductoAPI.Models;
using StackExchange.Redis;

namespace ProductoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ProductoContext _context;
        private readonly IConnectionMultiplexer _redis;

        public ProductosController(ProductoContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis;
        }

        // GET: api/Productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductoSet()
        {
            var db = _redis.GetDatabase();
            string cacheKey = "productoList";
            var productosCache = await db.StringGetAsync(cacheKey);

            if (!productosCache.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<List<Producto>>(productosCache);
            }

            var productos = await _context.Productos.ToListAsync();
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(productos), TimeSpan.FromMinutes(10));

            return productos;
        }

        // GET: api/Productos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var db = _redis.GetDatabase();
            string cacheKey = $"producto_{id}";
            var productoCache = await db.StringGetAsync(cacheKey);

            if (!productoCache.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<Producto>(productoCache);
            }

            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(producto), TimeSpan.FromMinutes(10));
            return producto;
        }

        // PUT: api/Productos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProducto(int id, Producto producto)
        {
            if (id != producto.Id)
            {
                return BadRequest();
            }

            _context.Entry(producto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                var db = _redis.GetDatabase();
                string cacheKeyProducto = $"producto_{id}";
                string cacheKeyList = "productoList";

                await db.KeyDeleteAsync(cacheKeyProducto);
                await db.KeyDeleteAsync(cacheKeyList);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id))
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

        // POST: api/Productos
        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            var db = _redis.GetDatabase();
            string cacheKeyList = "productoList";
            await db.KeyDeleteAsync(cacheKeyList);

            return CreatedAtAction("GetProducto", new { id = producto.Id }, producto);
        }

        // DELETE: api/Productos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            var db = _redis.GetDatabase();
            string cacheKeyProducto = $"producto_{id}";
            string cacheKeyList = "productoList";

            await db.KeyDeleteAsync(cacheKeyProducto);
            await db.KeyDeleteAsync(cacheKeyList);

            return NoContent();
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}
