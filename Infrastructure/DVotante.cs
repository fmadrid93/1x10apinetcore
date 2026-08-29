using Infrastructure;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

public class DVotante : DbHelper
{
    private static bool _columnasPasoPCVerificadas = false;

    /// <summary>
    /// Auto-migración perezosa (mismo patrón que DConfiguracion/DTerritorio):
    /// agrega las columnas de "Pasó por el PC" a TB_Votante la primera vez que
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

    public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null)
    {
        AsegurarColumnasPasoPorElPC();
        string t = (texto ?? "").Trim();
        string? rec = string.IsNullOrWhiteSpace(idRecinto) ? null : idRecinto.Trim();
        string? mesa = string.IsNullOrWhiteSpace(nroMesa) ? null : nroMesa.Trim();

        try
        {
            return EjecutarPA(
                "PA_VOTANTE_BUSCAR_PADRON_GLOBAL",
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = t },
                new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)rec ?? DBNull.Value },
                new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)mesa ?? DBNull.Value }
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
        AsegurarColumnasPasoPorElPC();

        string sql = @"
            UPDATE TB_Votante
            SET PasoPorElPC = 1,
                FechaPasoPorElPC = GETDATE(),
                IdUsuarioMarcaPasoPC = @IdUsuarioMarca
            WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));
            SELECT @@ROWCOUNT AS Filas;";

        var dt = EjecutarSQL(sql,
            new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca });

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["Filas"] != DBNull.Value)
        {
            return Convert.ToInt32(dt.Rows[0]["Filas"]);
        }
        return 0;
    }

    /// <summary>
    /// PA_VOTANTE_MARCAR_YA_VOTO solo devuelve el conteo de filas afectadas, no
    /// el CI. Lo necesitamos aparte para poder sincronizar el estado con
    /// PersonaMovilizada (ver VotanteService.MarcarYaVoto).
    /// </summary>
    public string? ObtenerCIPorId(string idVotante)
    {
        string sql = @"
            SELECT TOP 1 CI
            FROM TB_Votante WITH (NOLOCK)
            WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante))";

        var dt = EjecutarSQL(sql, new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value });
        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["CI"] != DBNull.Value)
        {
            string ci = dt.Rows[0]["CI"].ToString()?.Trim() ?? "";
            return string.IsNullOrEmpty(ci) ? null : ci;
        }
        return null;
    }

    public DataTable ObtenerTop10(int? idTerritorio = null)
    {
        return BuscarPadronGlobal("", null, null);
    }
}