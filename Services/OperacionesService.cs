using Microsoft.EntityFrameworkCore;
using WebApiAlmacen.Models;

namespace WebApiAlmacen.Services
{
    public class OperacionesService
    {
        private readonly MiAlmacenContext _context;
        private readonly IHttpContextAccessor _accessor;

        public OperacionesService(MiAlmacenContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _accessor = accessor;
        }

        public async Task<bool> CheckOperacion()
        {
            var ip = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();

            var ultimaOperacion = await _context.Operaciones.Where(x => x.Ip == ip)
                .OrderByDescending(x => x.FechaAccion).FirstOrDefaultAsync();

            if (ultimaOperacion == null)
            {
                return true;
            }

            var permitido = (DateTime.Now - ultimaOperacion.FechaAccion).TotalSeconds > 30;

            return permitido;
        }

        public async Task<bool> SePuedeInsertarLaOperacion()
        {
            int intervaloDeTiempoPermitidoEnSegundos = 30;
            var fechaUltimoRegistro = await _context.Operaciones.OrderByDescending(o => o.FechaAccion).Select(o => o.FechaAccion).FirstOrDefaultAsync();
            var diferencia = (DateTime.Now - fechaUltimoRegistro).TotalSeconds;
            if (diferencia < intervaloDeTiempoPermitidoEnSegundos)
            {
                return false;
            }
            return true;
        }

        public async Task AddOperacion(string operacion, string controller)
        {
            Operacione nuevaOperacion = new Operacione()
            {
                FechaAccion = DateTime.Now,
                Operacion = operacion,
                Controller = controller,
                Ip = _accessor.HttpContext.Connection.RemoteIpAddress.ToString()
            };

            await _context.Operaciones.AddAsync(nuevaOperacion);
            await _context.SaveChangesAsync();

            Task.FromResult(0);
        }
    }

}
