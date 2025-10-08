using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiAlmacen.DTOs;
using WebApiAlmacen.Models;
using WebApiAlmacen.Repository;

namespace WebApiAlmacen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FamiliaRepositoryController : ControllerBase
    {
        private readonly IFamiliaRepository _familiaRepository;
        public FamiliaRepositoryController(IFamiliaRepository familiaRepository)
        {
            _familiaRepository = familiaRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Familia>>> GetFamilias()
        {
            var familias = await _familiaRepository.GetFamilias();
            return Ok(familias);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Familia>> GetFamiliaById(int id)
        {
            var familia = await _familiaRepository.GetFamiliaById(id);
            if (familia == null)
                return NotFound();
            return Ok(familia);
        }

        [HttpPost]
        public async Task<ActionResult> AddFamilia(DTOFamilia familia)
        {
            var newFamilia = new Familia
            {
                Nombre = familia.Nombre,
            };
            await _familiaRepository.AddFamilia(newFamilia);
            return CreatedAtAction(nameof(GetFamiliaById), new { id = newFamilia.Id }, newFamilia);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFamilia(int id, DTOFamilia familia)
        {
            var familiaUpdate = await _familiaRepository.GetFamiliaById(id);
            if (familia == null)
                return NotFound();
            familiaUpdate.Nombre = familia.Nombre;
            await _familiaRepository.UpdateFamilia(familiaUpdate);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamilia(int id)
        {
            var familia = await _familiaRepository.GetFamiliaById(id);
            if (familia == null)
                return NotFound();
            await _familiaRepository.DeleteFamilia(id);
            return Ok();
        }

    }
}
