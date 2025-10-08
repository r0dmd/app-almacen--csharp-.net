using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiAlmacen.Filters
{
    public class ModifyResponseFilter : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult objectResult && objectResult.Value != null)
            {
                var originalData = objectResult.Value;

                // Modificar la respuesta agregando una nueva propiedad
                var modifiedResponse = new 
                {
                    data = originalData,
                    hora = DateTime.Now,
                };

                objectResult.Value = modifiedResponse;
            }

            base.OnResultExecuting(context);
        }
    }


}
