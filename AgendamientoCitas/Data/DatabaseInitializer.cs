using Dapper;

namespace AgendamientoCitas.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SqlConnectionFactory>();
        using var connection = db.CreateConnection();

        await connection.ExecuteAsync("""
            IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Clientes (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
                    Nombres nvarchar(120) NOT NULL,
                    Apellidos nvarchar(120) NOT NULL,
                    Identificacion nvarchar(30) NULL,
                    Telefono nvarchar(30) NULL,
                    Email nvarchar(180) NULL,
                    FechaCreacion datetime2 NOT NULL CONSTRAINT DF_Clientes_FechaCreacion DEFAULT SYSUTCDATETIME(),
                    Activo bit NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT 1
                );

                CREATE UNIQUE INDEX IX_Clientes_Identificacion
                ON dbo.Clientes (Identificacion)
                WHERE Identificacion IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Servicios', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Servicios (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Servicios PRIMARY KEY,
                    Nombre nvarchar(120) NOT NULL,
                    Descripcion nvarchar(500) NULL,
                    Precio decimal(18,2) NOT NULL,
                    DuracionMinutos int NOT NULL,
                    Activo bit NOT NULL CONSTRAINT DF_Servicios_Activo DEFAULT 1
                );
            END;

            IF OBJECT_ID('dbo.Citas', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Citas (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Citas PRIMARY KEY,
                    ClienteId int NOT NULL,
                    ServicioId int NOT NULL,
                    FechaInicio datetime2 NOT NULL,
                    FechaFin datetime2 NOT NULL,
                    Estado nvarchar(30) NOT NULL,
                    Motivo nvarchar(250) NULL,
                    Observaciones nvarchar(1000) NULL,
                    FechaCreacion datetime2 NOT NULL CONSTRAINT DF_Citas_FechaCreacion DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT FK_Citas_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id),
                    CONSTRAINT FK_Citas_Servicios FOREIGN KEY (ServicioId) REFERENCES dbo.Servicios(Id)
                );

                CREATE INDEX IX_Citas_ClienteId ON dbo.Citas (ClienteId);
                CREATE INDEX IX_Citas_ServicioId ON dbo.Citas (ServicioId);
                CREATE INDEX IX_Citas_FechaInicio ON dbo.Citas (FechaInicio);
            END;

            IF OBJECT_ID('dbo.Ingresos', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Ingresos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ingresos PRIMARY KEY,
                    CitaId int NULL,
                    ClienteId int NULL,
                    Concepto nvarchar(180) NOT NULL,
                    Monto decimal(18,2) NOT NULL,
                    MetodoPago nvarchar(30) NOT NULL,
                    FechaPago datetime2 NOT NULL,
                    Referencia nvarchar(120) NULL,
                    Notas nvarchar(500) NULL,
                    CONSTRAINT FK_Ingresos_Citas FOREIGN KEY (CitaId) REFERENCES dbo.Citas(Id) ON DELETE SET NULL,
                    CONSTRAINT FK_Ingresos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id) ON DELETE SET NULL
                );

                CREATE INDEX IX_Ingresos_CitaId ON dbo.Ingresos (CitaId);
                CREATE INDEX IX_Ingresos_ClienteId ON dbo.Ingresos (ClienteId);
            END;

            IF OBJECT_ID('dbo.Gastos', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Gastos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Gastos PRIMARY KEY,
                    Concepto nvarchar(180) NOT NULL,
                    Categoria nvarchar(120) NOT NULL,
                    Monto decimal(18,2) NOT NULL,
                    MetodoPago nvarchar(30) NOT NULL,
                    FechaGasto datetime2 NOT NULL,
                    Referencia nvarchar(120) NULL,
                    Notas nvarchar(500) NULL
                );

                CREATE INDEX IX_Gastos_FechaGasto ON dbo.Gastos (FechaGasto);
                CREATE INDEX IX_Gastos_Categoria ON dbo.Gastos (Categoria);
            END;
            """);
    }
}
