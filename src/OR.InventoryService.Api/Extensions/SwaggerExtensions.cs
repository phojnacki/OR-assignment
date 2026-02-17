using Microsoft.OpenApi;

namespace OR.InventoryService.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Pure token without 'Bearer' prefix",
                Name = "Authorization",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(doc => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc),
                    new List<string>()
                }
            });
            
            c.MapType<Models.AddInventoryRequest>(() => new Microsoft.OpenApi.OpenApiSchema
            {
                Type = Microsoft.OpenApi.JsonSchemaType.Object,
                Example = System.Text.Json.Nodes.JsonNode.Parse("""
                {
                  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "quantity": 1
                }
                """)
            });
            
            c.MapType<Models.TokenRequest>(() => new Microsoft.OpenApi.OpenApiSchema
            {
                Type = Microsoft.OpenApi.JsonSchemaType.Object,
                Example = System.Text.Json.Nodes.JsonNode.Parse("""
                {
                  "username": "OR",
                  "password": "or-secret",
                  "roles": ["read", "write"]
                }
                """)
            });
        });
        
        return services;
    }
}
