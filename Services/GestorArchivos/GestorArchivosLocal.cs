
using Microsoft.AspNetCore.StaticFiles;

namespace WebApiAlmacen.Services.GestorArchivos
{
    public class GestorArchivosLocal : IGestorArchivos
    {
        private readonly IWebHostEnvironment env; // Para poder localizar wwwroot
        private readonly IHttpContextAccessor httpContextAccessor; // Para conocer la configuración del servidor para construir la url de la imagen

        public GestorArchivosLocal(IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            this.env = env;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task BorrarArchivo(string ruta, string carpeta, bool publico)
        {
            string folder = publico ? $"{env.WebRootPath}\\{carpeta}" : $"{env.ContentRootPath}\\{carpeta}";
            if (ruta != null)
            {
                var nombreArchivo = Path.GetFileName(ruta);
                string directorioArchivo = Path.Combine(folder, nombreArchivo);

                if (File.Exists(directorioArchivo))
                {
                    File.Delete(directorioArchivo);
                }
            }

            return Task.FromResult(0);
        }

        public async Task<string> EditarArchivo(byte[] contenido, string extension, string carpeta, string ruta, bool publico)
        {
            await BorrarArchivo(ruta, carpeta, publico);
            return await GuardarArchivo(contenido, extension, carpeta, publico);
        }

        public async Task<string> GuardarArchivoNombreNativo(byte[] contenido, string nombreArchivo, string carpeta, bool publico)
        {
            string folder = publico ? $"{env.WebRootPath}\\{carpeta}" : $"{env.ContentRootPath}\\{carpeta}";
            // Creamos un nombre aleatorio con la extensión

            // Si no existe la carpeta la creamos
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // La ruta donde dejaremos el archivo será la concatenación de la ruta de la carpeta y el nombre del archivo
            string ruta = Path.Combine(folder, nombreArchivo);
            // Guardamos el archivo
            await File.WriteAllBytesAsync(ruta, contenido);

            // La url de la ímagen será http o https://dominio/carpeta/nombreimagen
            var urlServidor = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
            var urlParaBD = publico ? Path.Combine(urlServidor, carpeta, nombreArchivo).Replace("\\", "/") : nombreArchivo;
            return urlParaBD;
        }

        public async Task<string> GuardarArchivo(byte[] contenido, string extension, string carpeta, bool publico)
        {
            string folder = publico ? $"{env.WebRootPath}\\{carpeta}" : $"{env.ContentRootPath}\\{carpeta}";
            // Creamos un nombre aleatorio con la extensión
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";

            // Si no existe la carpeta la creamos
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // La ruta donde dejaremos el archivo será la concatenación de la ruta de la carpeta y el nombre del archivo
            string ruta = Path.Combine(folder, nombreArchivo);
            // Guardamos el archivo
            await File.WriteAllBytesAsync(ruta, contenido);

            // La url de la ímagen será http o https://dominio/carpeta/nombreimagen
            var urlServidor = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
            var urlParaBD = publico ? Path.Combine(urlServidor, carpeta, nombreArchivo).Replace("\\", "/") : nombreArchivo;
            return urlParaBD;
        }

        public async Task<byte[]> DescargarArchivo(string ruta, string carpeta, bool publico)
        {
            string folder = publico ? $"{env.WebRootPath}\\{carpeta}" : $"{env.ContentRootPath}\\{carpeta}";
            string rutaArchivo = Path.Combine(folder, Path.GetFileName(ruta));

            // Verifica si el archivo existe
            if (File.Exists(rutaArchivo))
            {
                // Obtiene el contenido del archivo
                var fileBytes = await File.ReadAllBytesAsync(rutaArchivo);

                return fileBytes;
            }
            return null;
        }

        public string GetMimeType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(Path.GetFileName(filePath), out string contentType))
            {
                contentType = "application/octet-stream"; // Tipo genérico en caso de no encontrar el tipo MIME
            }
            return contentType;
        }
    }
}
