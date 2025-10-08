using WebApiAlmacen.Models;

namespace WebApiAlmacen.Repository
{
    public interface IFamiliaRepository
    {
        Task<List<Familia>> GetFamilias();
        Task<Familia> GetFamiliaById(int id);
        Task AddFamilia(Familia familia);
        Task UpdateFamilia(Familia familia);
        Task DeleteFamilia(int id);
    }
}
