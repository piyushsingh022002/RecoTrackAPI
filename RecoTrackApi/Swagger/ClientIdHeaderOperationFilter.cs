using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RecoTrackApi.Swagger
{
    public class ClientIdHeaderOperationFilter : IOperationFilter
    {
        private readonly string? _defaultClientId;

        public ClientIdHeaderOperationFilter(IConfiguration configuration)
        {
            var allowedClients = configuration.GetSection("ClientSettings:AllowedClients").Get<string[]>() ?? Array.Empty<string>();
            _defaultClientId = allowedClients.FirstOrDefault(client => !string.IsNullOrWhiteSpace(client))?.Trim();
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            //Avoid Adding Duplicate Header Parameters
            if(operation.Parameters.Any(p => p.Name == "X-Client-Id"))
            {
                return;
            }

            var schema = new OpenApiSchema
            {
                Type = "string"
            };

            if (!string.IsNullOrWhiteSpace(_defaultClientId))
            {
                var defaultValue = new OpenApiString(_defaultClientId);
                schema.Default = defaultValue;
                schema.Example = defaultValue;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Client-Id",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Client Identifier(e.g. web-ui, mobile-app, postman)",
                Schema = schema
            });
        }
    }
}
