using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RecoTrackApi.Swagger
{
    public class ClientIdHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            //Avoid Adding Duplicate Header Parameters
            if(operation.Parameters.Any(p => p.Name == "X-Client-Id"))
            {
                return;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Client-Id",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Client Identifier(e.g. web-ui, mobile-app, postman)",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }
    }
}
