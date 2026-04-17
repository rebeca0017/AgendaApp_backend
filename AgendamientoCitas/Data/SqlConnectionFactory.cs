using Microsoft.Data.SqlClient;

namespace AgendamientoCitas.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No existe la cadena de conexion 'DefaultConnection'.");

    public SqlConnection CreateConnection() => new(_connectionString);
}
