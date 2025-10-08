using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Net;
using WebApiAlmacen.DTOs;
using WebApiAlmacen.Filters;
using WebApiAlmacen.Models;
using WebApiAlmacen.Services;


namespace WebApiAlmacen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
   // [TypeFilter(typeof(ExceptionFilter))] Innecesario porque lo tenemos registrado de forma global en Program
    public class FamiliasController : ControllerBase
    {
        private readonly MiAlmacenContext _context;
        private readonly ContadorConsultaFamilias _contadorConsultaFamilias;
        private readonly OperacionesService _operacionesService;
        public FamiliasController(MiAlmacenContext context, ContadorConsultaFamilias contadorConsultaFamilias,
            OperacionesService operacionesService)
        {
            _context = context;
            _contadorConsultaFamilias = contadorConsultaFamilias;
            _operacionesService = operacionesService;
        }

        [HttpGet]
       // [TypeFilter(typeof(ExceptionFilter))]
        public async Task<List<Familia>> GetFamilias()
        {
            //var context = new MiAlmacenContext();
            var familias = await _context.Familias.ToListAsync();

            _contadorConsultaFamilias.Add();
            var contador = _contadorConsultaFamilias.GetTotal();

            await _operacionesService.AddOperacion("Consulta", "Familias");

            return familias;
        }

        [HttpGet("sync")]
        public List<Familia> GetFamiliasSincrono()
        {
            // var context = new MiAlmacenContext();
            var familias = _context.Familias.ToList();

            return familias;
        }

        [HttpGet("/familias")]
        public async Task<List<Familia>> GetFamiliasRutaAbsoluta()
        {
            var familias = await _context.Familias.ToListAsync();
            await _operacionesService.AddOperacion("Consulta", "Familias");
            return familias;
        }

        [HttpGet("actionresult")]
        public async Task<ActionResult<List<Familia>>> GetFamiliasActionResult()
        {
            try
            {
                var lista = await _context.Familias.ToListAsync();
                var permitido = await _operacionesService.CheckOperacion();
                if (!permitido)
                {
                    return BadRequest("Operación no permitida");
                }

                await _operacionesService.AddOperacion("Consulta", "Familias");
                return Ok(lista);
            }
            catch (Exception ex)
            {
                //  return StatusCode(StatusCodes.Status500InternalServerError);
                return new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Content = "Se ha producido un error de acceso a la base de datos " + ex.Message
                };
            }
        }

        [HttpGet("tracking")]
        public async Task<ActionResult> GetFamiliasTracking()
        {
            var familia1 = await _context.Familias.AsTracking().Where(x => x.Id == 1).FirstOrDefaultAsync();
            familia1.Nombre = "Tecnología";
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("porid/{id:int}")] // api/familias/1 -->Si llamamos a api/familias/juan da 404 por la restricción
        public async Task<ActionResult<Familia>> GetFamiliaPorId([FromRoute] int id)
        {
            var familia = await _context.Familias.FindAsync(id);
            if (familia == null)
            {
                return NotFound();
            }
            return Ok(familia);
        }

        //[HttpGet("{contiene}")]
        [HttpGet("{contiene}/{param2?}")] // api/familias/a/b  --> param2 es opcional por el ?
        //[HttpGet("{contiene}/{param2=moda}")] // api/familias/a/b  --> param2 tiene el valor por defecto hogar
        // public async Task<ActionResult<Familia>> PrimeraFamiliaPorContiene(string contiene, string param2)
        public async Task<ActionResult<Familia>> GetPrimeraFamiliaPorContiene(string contiene, string? param2)
        {
            var familia = await _context.Familias.FirstOrDefaultAsync(x => x.Nombre.Contains(contiene) ||
                x.Nombre.Contains(param2));
            if (familia == null)
            {
                return NotFound();
            }
            return Ok(familia);
        }

        [HttpGet("parametrocontienequerystring")] // api/familias/parametrocontienequerystring?contiene=tec
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamiliasContieneQueryString([FromQuery] string contiene)
        {
            var familias = await _context.Familias.Where(x => x.Nombre.Contains(contiene)).ToListAsync();
            return Ok(familias);
        }

        [HttpGet("ordennombre/{desc}")]
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamiliasOrdenNombre(bool desc)
        {
            List<Familia> familias = new List<Familia>();
            if (!desc)
            {
                familias = await _context.Familias.OrderBy(x => x.Nombre).ToListAsync();
            }
            else
            {
                familias = await _context.Familias.OrderByDescending(x => x.Nombre).ToListAsync();
            }

            return Ok(familias);
        }

        [AllowAnonymous]
        [HttpGet("paginacion")]
        public async Task<ActionResult> GetFamiliasPaginacion()
        {
            var familias = await _context.Familias.Take(2).ToListAsync();
            var familias2 = await _context.Familias.Skip(1).Take(2).ToListAsync();
            return Ok(new { take = familias, takeSkip = familias2 });
        }

        [HttpGet("paginacion2/{pagina?}")]
        public async Task<ActionResult> GetFamiliasPaginacionPersonalizada(int pagina = 1)
        {
            int registrosPorPagina = 2;
            var familias = await _context.Familias.Skip((pagina - 1) * registrosPorPagina).Take(registrosPorPagina).ToListAsync();
            return Ok(familias);
        }

        [HttpGet("seleccioncamposdto")]
        public async Task<ActionResult> GetFamiliasSeleccionCamposDTO()
        {
            var familias = await _context.Familias.Select(x => new DTOFamilia { Id = x.Id, Nombre = x.Nombre }).ToListAsync();
            var familias2 = await (from x in _context.Familias
                                   select new DTOFamilia
                                   {
                                       Id = x.Id,
                                       Nombre = x.Nombre
                                   }).ToListAsync();
            return Ok(new { familias = familias, familias2 = familias2 });
        }

        [HttpGet("familiasproductos/{id:int}")]
        public async Task<ActionResult<Familia>> GetFamiliasProductosEager(int id)
        {
            // Familia llama a producto y producto a familia, lo que provoca un ciclo infinito del que informa swagger.
            // Por eso, hay que ir al Program y el la configuración de los controllers determinar que se ignoren los ciclos
            // Con ThenInclude podemos profundizar más en las relaciones
            var familia = await _context.Familias.Include(x => x.Productos).FirstOrDefaultAsync(x => x.Id == id);
            if (familia == null)
            {
                return NotFound();
            }
            return Ok(familia);
        }

        [HttpGet("familiasproductosselect/{id:int}")]
        public async Task<ActionResult<Familia>> GetFamiliasProductosSelect(int id)
        {
            var familia = await _context.Familias
                .Select(x => new DTOFamiliaProducto
                {
                    IdFamilia = x.Id,
                    Nombre = x.Nombre,
                    TotalProductos = x.Productos.Count(),
                    PrecioPromedio = x.Productos.Average(y => y.Precio),
                    Productos = x.Productos.Select(y => new DTOProductoItem
                    {
                        IdProducto = y.Id,
                        Nombre = y.Nombre
                    }).ToList(),
                }).FirstOrDefaultAsync(x => x.IdFamilia == id);

            //var familia = await (from x in _context.Familias
            //                     select new DTOFamiliaProducto
            //                     {
            //                         IdFamilia = x.Id,
            //                         Nombre = x.Nombre,
            //                         TotalProductos = x.Productos.Count(),
            //                          PrecioPromedio = x.Productos.Average(y => y.Precio),
            //                         Productos = x.Productos.Select(y => new DTOProductoItem
            //                         {
            //                             IdProducto = y.Id,
            //                             Nombre = y.Nombre
            //                         }).ToList(),
            //                     }).FirstOrDefaultAsync(x => x.IdFamilia == id);

            if (familia == null)
            {
                return NotFound();
            }
            return Ok(familia);
        }

        [HttpPost]
        public async Task<ActionResult> PostFamilia(DTOFamilia familia)
        {
            var newFamilia = new Familia()
            {
                Nombre = familia.Nombre
            };

            //var estatus1 = _context.Entry(newFamilia).State;

            await _context.AddAsync(newFamilia);
            //var estatus2 = _context.Entry(newFamilia).State;

            await _context.SaveChangesAsync();
            //var estatus3 = _context.Entry(newFamilia).State;

            await _operacionesService.AddOperacion("Creación", "Familias");

            return Created("Familia", new { familia = newFamilia });
        }

        [HttpPost("varios")]
        public async Task<ActionResult> PostFamilias(DTOFamilia[] familias)
        {
            List<Familia> variasFamilias = new();
            foreach (var f in familias)
            {
                variasFamilias.Add(new Familia
                {
                    Nombre = f.Nombre
                });
            }
            await _context.AddRangeAsync(variasFamilias);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> PutFamilia([FromRoute] int id, [FromBody] DTOFamilia familia)
        {
            if (id != familia.Id)
            {
                return BadRequest("Los ids proporcionados son diferentes");
            }
            var familiaUpdate = await _context.Familias.FirstOrDefaultAsync(x => x.Id == id);
            if (familiaUpdate == null)
            {
                return NotFound();
            }
            familiaUpdate.Nombre = familia.Nombre;
            _context.Update(familiaUpdate);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            //var hayProductos = await _context.Productos.AnyAsync(x => x.FamiliaId == id);
            //if (hayProductos)
            //{
            //    return BadRequest("Hay productos relacionados");
            //}

            //// Método clásico
            //var familia = await _context.Familias.FindAsync(id);
            //if (familia == null)
            //{
            //    return NotFound();
            //}

            //_context.Familias.Remove(familia);

            // Método moderno
            var familia = await _context.Familias.Where(x => x.Id == id).ExecuteDeleteAsync();
            if (familia == 0)
            {
                return NotFound();
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Ejemplo de consultas SQL
        /// </summary>
        [HttpGet("sql/{id:int}")]
        public async Task<ActionResult<Familia>> FamiliaPorIdSQL(int id)
        {
            //var familiax = await _context.Familias.FindAsync(id);
            var familia = await _context.Familias
                        .FromSqlInterpolated($"SELECT * FROM Familias WHERE Id = {id}")
                        .FirstOrDefaultAsync();

            if (familia == null)
            {
                return NotFound();
            }
            return Ok(familia);
        }

        [HttpPost("sql")]
        public async Task<ActionResult> Post(DTOFamilia familia)
        {
            //Ejemplo de sentencia SQL de inserción	
            await _context.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO Familias(Nombre) VALUES({familia.Nombre})");
            return Ok();
        }
    }
}
