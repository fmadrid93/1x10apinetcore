using Infrastructure;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

public class DVotante : DbHelper
{
    private static bool _columnasPasoPCVerificadas = false;

    /// <summary>
    /// Auto-migración perezosa:
    /// agrega las columnas de "Pasó por el PC" y "Combustible" a TB_Votante la primera vez que
    /// se necesitan, sin requerir un script manual aparte.
    /// </summary>
    private void AsegurarColumnasPasoPorElPC()
    {
        if (_columnasPasoPCVerificadas) return;
        try
        {
            string sql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TB_Votante') AND name = 'PasoPorElPC')
                BEGIN
                    ALTER TABLE TB_Votante ADD PasoPorElPC BIT NULL, FechaPasoPorElPC DATETIME NULL, IdUsuarioMarcaPasoPC INT NULL;
                END
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TB_Votante') AND name = 'Combustible')
                BEGIN
                    ALTER TABLE TB_Votante ADD Combustible BIT NULL, FechaCombustible DATETIME NULL, IdUsuarioMarcaCombustible INT NULL;
                END";
            EjecutarSQL(sql);
            _columnasPasoPCVerificadas = true;
        }
        catch
        {
            // Si falla por permisos, se sigue sin esta funcionalidad en vez de tumbar el resto.
        }
    }

    public DataTable ObtenerVotante(string ci)
    {
        AsegurarColumnasPasoPorElPC();
        return EjecutarPA(
            "PA_ObtenerVotante",
            new SqlParameter("@CI", SqlDbType.VarChar, 100) { Value = (object?)ci?.Trim() ?? DBNull.Value }
        );
    }

    public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null, int? idTerritorio = null)
    {
        AsegurarColumnasPasoPorElPC();
        string t = (texto ?? "").Trim();
        //string? rec = string.IsNullOrWhiteSpace(idRecinto) ? null : idRecinto.Trim();
        //string? mesa = string.IsNullOrWhiteSpace(nroMesa) ? null : nroMesa.Trim();

        try
        {
            return EjecutarPA(
                "PA_VOTANTE_BUSCAR_PADRON_GLOBAL",
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = t },
                   //new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)rec ?? DBNull.Value },
                   //new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)mesa ?? DBNull.Value },
                   new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
                new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa ?? DBNull.Value },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
            );
        }
        catch
        {
            return EjecutarPA(
                "PA_VOTANTE_BUSCAR_PADRON_GLOBAL",
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = t }
            );
        }
    }

    public DataTable MarcarYaVoto(string idVotante, int idUsuarioMarca, string? observacion)
    {
        return EjecutarPA(
            "PA_VOTANTE_MARCAR_YA_VOTO",
            new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
            new SqlParameter("@Observacion", SqlDbType.VarChar, 300) { Value = (object?)observacion ?? DBNull.Value }
        );
    }

    /// <summary>
    /// Marca "Pasó por el PC" (checkpoint distinto de "Ya Votó"): no pisa
    /// EstadoDiaD, queda en columnas propias para no mezclar los dos conceptos.
    /// </summary>
    public int MarcarPasoPorElPC(string idVotante, int idUsuarioMarca)
    {
        var dt = EjecutarPA(
            "PA_VOTANTE_MARCAR_PASO_PC",
            new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca }
        );

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["FilasAfectadas"] != DBNull.Value)
        {
            return Convert.ToInt32(dt.Rows[0]["FilasAfectadas"]);
        }
        return 0;
    }

    /// <summary>
    /// Marca o desmarca entrega de Combustible para el votante (específico para Natalio / Checkpoint PC).
    /// </summary>
    public int MarcarCombustible(string idVotante, int idUsuarioMarca, bool recibioCombustible)
    {
        AsegurarColumnasPasoPorElPC();
        string sql = @"
            DECLARE @IdGuid UNIQUEIDENTIFIER = TRY_CAST(LTRIM(RTRIM(@IdVotante)) AS UNIQUEIDENTIFIER);
            IF (@IdGuid IS NOT NULL)
            BEGIN
                UPDATE TB_Votante
                SET Combustible = @Combustible,
                    FechaCombustible = CASE WHEN @Combustible = 1 THEN GETDATE() ELSE NULL END,
                    IdUsuarioMarcaCombustible = CASE WHEN @Combustible = 1 THEN @IdUsuarioMarca ELSE NULL END
                WHERE IdVotante = @IdGuid;
                SELECT @@ROWCOUNT AS FilasAfectadas;
            END
            ELSE
            BEGIN
                UPDATE TB_Votante
                SET Combustible = @Combustible,
                    FechaCombustible = CASE WHEN @Combustible = 1 THEN GETDATE() ELSE NULL END,
                    IdUsuarioMarcaCombustible = CASE WHEN @Combustible = 1 THEN @IdUsuarioMarca ELSE NULL END
                WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));
                SELECT @@ROWCOUNT AS FilasAfectadas;
            END";

        var dt = EjecutarSQL(
            sql,
            new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
            new SqlParameter("@Combustible", SqlDbType.Bit) { Value = recibioCombustible }
        );

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["FilasAfectadas"] != DBNull.Value)
        {
            return Convert.ToInt32(dt.Rows[0]["FilasAfectadas"]);
        }
        return 0;
    }

    /// <summary>
    /// Obtiene el CI por IdVotante usando búsqueda optimizada por índice.
    /// </summary>
    public string? ObtenerCIPorId(string idVotante)
    {
        string sql = @"
            DECLARE @IdGuid UNIQUEIDENTIFIER = TRY_CAST(LTRIM(RTRIM(@IdVotante)) AS UNIQUEIDENTIFIER);
            IF (@IdGuid IS NOT NULL)
            BEGIN
                SELECT TOP 1 CI
                FROM TB_Votante WITH (INDEX(IX_TB_Votante_IdVotante), NOLOCK)
                WHERE IdVotante = @IdGuid;
            END
            ELSE
            BEGIN
                SELECT TOP 1 CI
                FROM TB_Votante WITH (NOLOCK)
                WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));
            END";

        var dt = EjecutarSQL(sql, new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value });
        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["CI"] != DBNull.Value)
        {
            string ci = dt.Rows[0]["CI"].ToString()?.Trim() ?? "";
            return string.IsNullOrEmpty(ci) ? null : ci;
        }
        return null;
    }

    public DataTable RecintoDiadConteos(string idRecinto, int? idAdmin = null, string? nroMesa = null)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "CONTEOS" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = idRecinto.Trim() },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value }
        );
    }

    public DataTable RecintoPadronFaltan(string idRecinto, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "FALTAN_VOTAR_PADRON" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = idRecinto.Trim() },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto?.Trim() ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit }
        );
    }

    public DataTable RecintoVotaronNoRegistrados(string idRecinto, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "VOTARON_NO_REGISTRADOS" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = idRecinto.Trim() },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto?.Trim() ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit }
        );
    }

    public DataTable RecintoRegistradosFaltan(string idRecinto, int? idAdmin = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "REGISTRADOS_FALTAN" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = idRecinto.Trim() },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto?.Trim() ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit }
        );
    }

    public DataTable RecintoMesas(string idRecinto)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "MESAS" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = idRecinto.Trim() }
        );
    }

    public DataTable ObtenerTop10(int? idTerritorio = null)
    {
        return BuscarPadronGlobal("", null, null, idTerritorio);
    }

    public DataTable VeedoresRendimientoResumen(int? idTerritorio = null, int? idAdmin = null, int? idGerente = null)
    {
        return EjecutarPA(
            "pa_veedor_rendimiento_resumen",
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@IdGerente", SqlDbType.Int) { Value = (object?)idGerente ?? DBNull.Value }
        );
    }

    public DataTable VotantesMarcadosListar(
        int? idUsuarioMarca = null,
        string? tipoMarca = null,
        int? idTerritorio = null,
        int? idAdmin = null,
        int? idGerente = null,
        int? idMovilizador = null,
        string? texto = null,
        int offset = 0,
        int limit = 1000)
    {
        return EjecutarPA(
            "pa_veedor_votantes_marcados_listar",
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = (object?)idUsuarioMarca ?? DBNull.Value },
            new SqlParameter("@TipoMarca", SqlDbType.VarChar, 20) { Value = (object?)tipoMarca ?? DBNull.Value },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@IdGerente", SqlDbType.Int) { Value = (object?)idGerente ?? DBNull.Value },
            new SqlParameter("@IdMovilizador", SqlDbType.Int) { Value = (object?)idMovilizador ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit }
        );
    }
}