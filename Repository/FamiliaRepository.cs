using Microsoft.EntityFrameworkCore;
using WebApiAlmacen.Models;

namespace WebApiAlmacen.Repository
{
    public class FamiliaRepository : IFamiliaRepository
    {
        private readonly MiAlmacenContext _context;
        public FamiliaRepository(MiAlmacenContext context)
        {
            _context = context;
        }

        public async Task<List<Familia>> GetFamilias()
        {
            var familias = await _context.Familias.ToListAsync();
            return familias;
        }

        public async Task<Familia> GetFamiliaById(int id)
        {
            var familia = await _context.Familias.FindAsync(id);
            return familia;
        }

        public async Task AddFamilia(Familia familia)
        {
            await _context.Familias.AddAsync(familia);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateFamilia(Familia familia)
        {
            _context.Familias.Update(familia);
            _context.SaveChanges();
        }

        public async Task DeleteFamilia(int id)
        {
            var familia = await _context.Familias.FindAsync(id);
            if (familia != null)
            {
                _context.Familias.Remove(familia);
                await _context.SaveChangesAsync();
            }
        }
    }


}
