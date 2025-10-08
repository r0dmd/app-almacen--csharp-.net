using WebApiAlmacen.Classes;

namespace WebApiAlmacen.Services.Hash
{
    public interface IHashService
    {
        ResultadoHash Hash(string textoPlano);
        ResultadoHash Hash(string textoPlano, byte[] salt);
    }
}
