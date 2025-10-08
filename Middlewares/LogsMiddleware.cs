namespace WebApiAlmacen.Middlewares
{
    public class LogsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public LogsMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        // Invoke o InvokeAsync
        public async Task InvokeAsync(HttpContext httpContext)
        {
            var IP = httpContext.Connection.RemoteIpAddress.ToString();
            //if (IP == "::1") // Bloquearía las peticiones de una IP
            //{
            //    httpContext.Response.StatusCode = 401;
            //    return;
            //}
            var ruta = httpContext.Request.Path.ToString();
            var metodo = httpContext.Request.Method;

            var path = $@"{_env.ContentRootPath}\log.txt";
            using (StreamWriter writer = new StreamWriter(path, append: true))
            {
                writer.WriteLine($@"{IP} - {DateTime.Now} - {metodo} - {ruta}");
            }

            await _next(httpContext);
        }
    }
}
