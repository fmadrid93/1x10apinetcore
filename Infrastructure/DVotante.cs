using Infrastructure;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

public class DVotante : DbHelper
{
    public DataTable ObtenerVotante(string ci)
    {
        return EjecutarPA(
            "PA_ObtenerVotante",
            new SqlParameter("@CI", SqlDbType.VarChar, 100) { Value = (object?)ci?.Trim() ?? DBNull.Value }
        );
    }

    public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null)
    {
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

    public DataTable MarcarYaVoto(int idVotante, int idUsuarioMarca, string? observacion)
    {
        return EjecutarPA(
            "PA_VOTANTE_MARCAR_YA_VOTO",
            new SqlParameter("@IdVotante", SqlDbType.Int) { Value = idVotante },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
            new SqlParameter("@Observacion", SqlDbType.VarChar, 300) { Value = (object?)observacion ?? DBNull.Value }
        );
    }

    public DataTable ObtenerTop10(int? idTerritorio = null)
    {
        return BuscarPadronGlobal("", null, null);
    }
}