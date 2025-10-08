namespace WebApiAlmacen.Services.GestorArchivos
{
    public interface IGestorArchivos
    {
        Task<string> EditarArchivo(byte[] contenido, string extension, string carpeta, string ruta,
            bool publico);
        Task BorrarArchivo(string ruta, string carpeta, bool publico);
        Task<string> GuardarArchivo(byte[] contenido, string extension, string carpeta, bool publico);
        Task<byte[]> DescargarArchivo(string ruta, string carpeta, bool publico);
        string GetMimeType(string filePath);
        Task<string> GuardarArchivoNombreNativo(byte[] contenido, string nombreArchivo, string carpeta, bool publico);
    }
}
