using Microsoft.OpenApi;

namespace OR.ProductService.Api.Extensions;

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
            
            c.MapType<Models.CreateProductRequest>(() => new Microsoft.OpenApi.OpenApiSchema
            {
                Type = Microsoft.OpenApi.JsonSchemaType.Object,
                Example = System.Text.Json.Nodes.JsonNode.Parse("""
                {
                  "name": "Some Product",
                  "description": "Some product description",
                  "price": 99
                }
                """)
            });
        });
        
        return services;
    }
}
