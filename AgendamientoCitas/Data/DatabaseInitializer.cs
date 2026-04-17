using Dapper;

namespace AgendamientoCitas.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SqlConnectionFactory>();
        var configuration = services.GetRequiredService<IConfiguration>();
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

            IF OBJECT_ID('dbo.ConfiguracionApp', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ConfiguracionApp (
                    Id int NOT NULL CONSTRAINT PK_ConfiguracionApp PRIMARY KEY,
                    NombreApp nvarchar(120) NOT NULL,
                    Logo nvarchar(max) NULL,
                    FechaActualizacion datetime2 NOT NULL CONSTRAINT DF_ConfiguracionApp_FechaActualizacion DEFAULT SYSUTCDATETIME()
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionApp WHERE Id = 1)
            BEGIN
                INSERT INTO dbo.ConfiguracionApp (Id, NombreApp, Logo)
                VALUES (1, N'Mi Agenda', NULL);
            END;

            IF OBJECT_ID('dbo.Roles', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Roles (
                    Id nvarchar(450) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
                    Name nvarchar(256) NULL,
                    NormalizedName nvarchar(256) NULL,
                    ConcurrencyStamp nvarchar(max) NULL
                );

                CREATE UNIQUE INDEX RoleNameIndex ON dbo.Roles (NormalizedName)
                WHERE NormalizedName IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.RolesClaims', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.RolesClaims (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RolesClaims PRIMARY KEY,
                    RoleId nvarchar(450) NOT NULL,
                    ClaimType nvarchar(max) NULL,
                    ClaimValue nvarchar(max) NULL,
                    CONSTRAINT FK_RolesClaims_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
                );

                CREATE INDEX IX_RolesClaims_RoleId ON dbo.RolesClaims (RoleId);
            END;

            IF OBJECT_ID('dbo.UsuariosRoles', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.UsuariosRoles (
                    UserId nvarchar(450) NOT NULL,
                    RoleId nvarchar(450) NOT NULL,
                    CONSTRAINT PK_UsuariosRoles PRIMARY KEY (UserId, RoleId),
                    CONSTRAINT FK_UsuariosRoles_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_UsuariosRoles_Usuarios_UserId FOREIGN KEY (UserId) REFERENCES dbo.Usuarios(Id) ON DELETE CASCADE
                );

                CREATE INDEX IX_UsuariosRoles_RoleId ON dbo.UsuariosRoles (RoleId);
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'ADMIN')
            BEGIN
                INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
                VALUES (CONVERT(nvarchar(450), NEWID()), N'Admin', N'ADMIN', CONVERT(nvarchar(450), NEWID()));
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'USUARIO')
            BEGIN
                INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
                VALUES (CONVERT(nvarchar(450), NEWID()), N'Usuario', N'USUARIO', CONVERT(nvarchar(450), NEWID()));
            END;

            IF COL_LENGTH('dbo.Clientes', 'UsuarioId') IS NULL
            BEGIN
                ALTER TABLE dbo.Clientes ADD UsuarioId nvarchar(450) NULL;
            END;

            IF COL_LENGTH('dbo.Servicios', 'UsuarioId') IS NULL
            BEGIN
                ALTER TABLE dbo.Servicios ADD UsuarioId nvarchar(450) NULL;
            END;

            IF COL_LENGTH('dbo.Citas', 'UsuarioId') IS NULL
            BEGIN
                ALTER TABLE dbo.Citas ADD UsuarioId nvarchar(450) NULL;
            END;

            IF COL_LENGTH('dbo.Ingresos', 'UsuarioId') IS NULL
            BEGIN
                ALTER TABLE dbo.Ingresos ADD UsuarioId nvarchar(450) NULL;
            END;

            IF COL_LENGTH('dbo.Gastos', 'UsuarioId') IS NULL
            BEGIN
                ALTER TABLE dbo.Gastos ADD UsuarioId nvarchar(450) NULL;
            END;
            """);

        await connection.ExecuteAsync("""
            DECLARE @OwnerUserId nvarchar(450);

            SELECT TOP 1 @OwnerUserId = u.Id
            FROM dbo.Usuarios u
            WHERE NOT EXISTS (
                SELECT 1
                FROM dbo.UsuariosRoles ur
                INNER JOIN dbo.Roles r ON r.Id = ur.RoleId
                WHERE ur.UserId = u.Id AND r.NormalizedName = N'ADMIN'
            )
            ORDER BY u.Email;

            IF @OwnerUserId IS NULL
            BEGIN
                SELECT TOP 1 @OwnerUserId = Id
                FROM dbo.Usuarios
                ORDER BY Email;
            END;

            IF @OwnerUserId IS NOT NULL
            BEGIN
                UPDATE dbo.Clientes SET UsuarioId = @OwnerUserId WHERE UsuarioId IS NULL;
                UPDATE dbo.Servicios SET UsuarioId = @OwnerUserId WHERE UsuarioId IS NULL;
                UPDATE dbo.Citas SET UsuarioId = @OwnerUserId WHERE UsuarioId IS NULL;
                UPDATE dbo.Ingresos SET UsuarioId = @OwnerUserId WHERE UsuarioId IS NULL;
                UPDATE dbo.Gastos SET UsuarioId = @OwnerUserId WHERE UsuarioId IS NULL;
            END;

            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Clientes_Identificacion'
                  AND object_id = OBJECT_ID(N'dbo.Clientes')
            )
            BEGIN
                DROP INDEX IX_Clientes_Identificacion ON dbo.Clientes;
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Clientes_UsuarioId_Identificacion'
                  AND object_id = OBJECT_ID(N'dbo.Clientes')
            )
            BEGIN
                CREATE UNIQUE INDEX IX_Clientes_UsuarioId_Identificacion
                ON dbo.Clientes (UsuarioId, Identificacion)
                WHERE UsuarioId IS NOT NULL AND Identificacion IS NOT NULL;
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Clientes_UsuarioId'
                  AND object_id = OBJECT_ID(N'dbo.Clientes')
            )
            BEGIN
                CREATE INDEX IX_Clientes_UsuarioId ON dbo.Clientes (UsuarioId);
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Servicios_UsuarioId'
                  AND object_id = OBJECT_ID(N'dbo.Servicios')
            )
            BEGIN
                CREATE INDEX IX_Servicios_UsuarioId ON dbo.Servicios (UsuarioId);
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Citas_UsuarioId'
                  AND object_id = OBJECT_ID(N'dbo.Citas')
            )
            BEGIN
                CREATE INDEX IX_Citas_UsuarioId ON dbo.Citas (UsuarioId);
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Ingresos_UsuarioId'
                  AND object_id = OBJECT_ID(N'dbo.Ingresos')
            )
            BEGIN
                CREATE INDEX IX_Ingresos_UsuarioId ON dbo.Ingresos (UsuarioId);
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_Gastos_UsuarioId'
                  AND object_id = OBJECT_ID(N'dbo.Gastos')
            )
            BEGIN
                CREATE INDEX IX_Gastos_UsuarioId ON dbo.Gastos (UsuarioId);
            END;

            INSERT INTO dbo.UsuariosRoles (UserId, RoleId)
            SELECT u.Id, r.Id
            FROM dbo.Usuarios u
            CROSS JOIN dbo.Roles r
            WHERE r.NormalizedName = N'USUARIO'
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.UsuariosRoles ur
                  WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
              );
            """);

        var adminEmails = configuration.GetSection("Security:AdminEmails").Get<string[]>() ?? [];
        foreach (var email in adminEmails.Where(email => !string.IsNullOrWhiteSpace(email)))
        {
            await connection.ExecuteAsync("""
                INSERT INTO dbo.UsuariosRoles (UserId, RoleId)
                SELECT u.Id, r.Id
                FROM dbo.Usuarios u
                CROSS JOIN dbo.Roles r
                WHERE u.NormalizedEmail = UPPER(@Email)
                  AND r.NormalizedName = N'ADMIN'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.UsuariosRoles ur
                      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
                  );
                """, new { Email = email.Trim() });
        }
    }
}
