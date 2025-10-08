using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiAlmacen.Filters
{
    public class ExceptionFilter : ExceptionFilterAttribute
    {
        private readonly ILogger<ExceptionFilter> logger;
        private readonly IWebHostEnvironment env;

        public ExceptionFilter(ILogger<ExceptionFilter> logger, IWebHostEnvironment env)
        {
            this.logger = logger;
            this.env = env;
        }

        public override void OnException(ExceptionContext context)
        {
            logger.LogError(context.Exception, context.Exception.Message);

            var path = $@"{env.ContentRootPath}\errores.txt";
            using (StreamWriter writer = new StreamWriter(path, append: true))
            {
                writer.WriteLine(DateTime.Now + context.Exception.Message);
            }

            var response = new 
            {
                error = "Ocurrió un error interno en el servidor",
                message = context.Exception.Message
            };

            context.Result = new ObjectResult(response)
            {
                StatusCode = 500
            };

            context.ExceptionHandled = true;
        }
    }


}
