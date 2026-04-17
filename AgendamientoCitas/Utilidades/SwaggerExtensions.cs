using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace AgendamientoCitas.Utilidades
{
    public static class SwaggerExtension
    {
        public static TBuilder AgregarParametrosPaginacionAOpenAPI<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        {
            return builder.WithOpenApi(opciones =>
            {
                opciones.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = "pagina",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Query,
                    Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "integer",
                        Default = new OpenApiInteger(1)
                    }
                });

                opciones.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = "recordsPorPagina",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Query,
                    Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "integer",
                        Default = new OpenApiInteger(10)
                    }
                });
                return opciones;
            });
        }


        public static TBuilder AgregarParametrosTransferenciasFiltroAOpenAPI<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder
        {
            return builder.WithOpenApi(opciones =>
            {
                opciones.Parameters ??= new List<OpenApiParameter>();

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "pagina",
                    In = ParameterLocation.Query,
                    Description = "Número de página.",
                    Schema = new OpenApiSchema
                    {
                        Type = "integer",
                        Default = new OpenApiInteger(1)
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "recordsPorPagina",
                    In = ParameterLocation.Query,
                    Description = "Cantidad de registros por página.",
                    Schema = new OpenApiSchema
                    {
                        Type = "integer",
                        Default = new OpenApiInteger(10)
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "fechaDesde",
                    In = ParameterLocation.Query,
                    Description = "Fecha inicial del filtro.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "date"
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "fechaHasta",
                    In = ParameterLocation.Query,
                    Description = "Fecha final del filtro.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "date"
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "bodega",
                    In = ParameterLocation.Query,
                    Description = "Código o nombre de la bodega.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "ubicacion",
                    In = ParameterLocation.Query,
                    Description = "Ubicación asociada a la transferencia.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "codigoTransferencia",
                    In = ParameterLocation.Query,
                    Description = "Código o número de transferencia.",
                    Schema = new OpenApiSchema
                    {
                        Type = "integer"
                    }
                });

                opciones.Parameters.Add(new OpenApiParameter
                {
                    Name = "estado",
                    In = ParameterLocation.Query,
                    Description = "Estado de la transferencia.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });

                return opciones;
            });
        }
    }
}
