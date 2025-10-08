using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WebApiAlmacen.DTOs;
using WebApiAlmacen.Models;
using WebApiAlmacen.Services.GestorArchivos;

namespace WebApiAlmacen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly MiAlmacenContext _context;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly IOutputCacheStore _outputCacheStore;

        // Tenemos que etiquetar cada método donde implementamos caché
        private const string cacheProductos = "get_productos";
        public ProductosController(MiAlmacenContext context, IGestorArchivos gestorArchivos,
            IOutputCacheStore outputCacheStore)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _outputCacheStore = outputCacheStore;
        }

        [HttpGet("productosagrupadospordescatalogado")]
        public async Task<ActionResult> GetProductosAgrupadosPorDescatalogado()
        {
            var productos = await _context.Productos.GroupBy(g => g.Descatalogado)
                .Select(x => new
                {
                    Descatalogado = x.Key,
                    Total = x.Count(),
                    Productos = x.ToList()
                }).ToListAsync();

            return Ok(productos);
        }

        // Ejecución diferida 
        // Mala práctica (no diferida)
        [HttpGet("filtrar")]
        public async Task<ActionResult> GetProductosFiltado([FromQuery] DTOProductosFiltro condicion)
        {
            var productos = await _context.Productos.ToListAsync();

            if (condicion.Descatalogado)
            {
                productos = productos.Where(x => x.Descatalogado == condicion.Descatalogado).ToList();
            }

            if (!string.IsNullOrEmpty(condicion.Nombre))
            {
                productos = productos.Where(x => x.Nombre.Contains(condicion.Nombre)).ToList();
            }

            if (condicion.FamiliaId != 0)
            {
                productos = productos.Where(x => x.FamiliaId == condicion.FamiliaId).ToList();
            }

            return Ok(productos);
        }

        [HttpGet("filtrarasqueryable")]
        public async Task<ActionResult> GetFiltroMultiple([FromQuery] DTOProductosFiltro filtroProductos)
        {
            // AsQueryable nos permite ir construyendo paso a paso el filtrado y ejecutarlo al final.
            // Si lo convertimos a una lista (toListAsync) el resto de filtros los hacemos en memoria
            // porque toListAsync ya trae a la memoria del servidor los datos desde el servidor de base de datos
            // Hacer los filtros en memoria es menos eficiente que hacerlos en una base de datos.
            // Construimos los filtros de forma dinámica y hasta que no hacemos el ToListAsync no vamos a la base de datos
            // para traer la información
            var productosQueryable = _context.Productos.AsQueryable();

            if (!string.IsNullOrEmpty(filtroProductos.Nombre))
            {
                productosQueryable = productosQueryable.Where(x => x.Nombre.Contains(filtroProductos.Nombre));
            }

            if (filtroProductos.Descatalogado)
            {
                productosQueryable = productosQueryable.Where(x => x.Descatalogado);
            }

            if (filtroProductos.FamiliaId != 0)
            {
                productosQueryable = productosQueryable.Where(x => x.FamiliaId == filtroProductos.FamiliaId);
            }

            var productos = await productosQueryable.ToListAsync();

            return Ok(productos);
        }


        /// <summary>
        /// /////////////////
        /// Resultado de la creación automática del controller
        /// /// </summary>
        /// <returns></returns>
        // GET: api/Productos
        [HttpGet]
        [OutputCache(Tags = [cacheProductos])] // cacheProductos es la etiqueta definida arriba
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            return await _context.Productos.ToListAsync();
        }

        // GET: api/Productos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            return producto;
        }

        // PUT: api/Productos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProducto", new { id = producto.Id }, producto);
        }

        // DELETE: api/Productos/5
        [Authorize]
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

            return NoContent();
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }

        //////////
        /// Subir producto con archivos y otros métodos con archivos
        /////////

        [HttpPost("archivo")]
        public async Task<ActionResult> PostProductos([FromForm] DTOProductoAgregar producto)
        {
            Producto newProducto = new Producto
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Descatalogado = false,
                FechaAlta = DateOnly.FromDateTime(DateTime.Now),
                FamiliaId = producto.FamiliaId,
                FotoUrl = ""
            };

            if (producto.Foto != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Extraemos la imagen de la petición
                    await producto.Foto.CopyToAsync(memoryStream);
                    // La convertimos a un array de bytes que es lo que necesita el método de guardar
                    var contenido = memoryStream.ToArray();
                    // La extensión la necesitamos para guardar el archivo
                    var extension = Path.GetExtension(producto.Foto.FileName);
                    // Recibimos el nombre del archivo
                    // El servicio Transient GestorArchivosLocal instancia el servicio y cuando se deja de usar se destruye
                    newProducto.FotoUrl = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes", false);
                    //newProducto.FotoUrl = await _gestorArchivos.GuardarArchivoNombreNativo(contenido, producto.Foto.FileName, "imagenes", true);
                }
            }

            await _context.AddAsync(newProducto);
            await _context.SaveChangesAsync();

            // Deshacer caché
            await _outputCacheStore.EvictByTagAsync(cacheProductos, default);

            return Ok(newProducto);
        }

        [HttpDelete("archivo/{id}")]
        public async Task<ActionResult> DeleteProductos([FromRoute] int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            await _gestorArchivos.BorrarArchivo(producto.FotoUrl, "imagenes", false);
            _context.Remove(producto);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("downloadimage/{id}")]
        public async Task<ActionResult> DownloadImage(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var imagen = await _gestorArchivos.DescargarArchivo(producto.FotoUrl, "imagenes", false);
            var mimeType = _gestorArchivos.GetMimeType(producto.FotoUrl);
            return new FileContentResult(imagen, mimeType);
        }

    }
}
