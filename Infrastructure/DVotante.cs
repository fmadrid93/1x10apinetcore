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
            new SqlParameter("@CI", SqlDbType.VarChar, 100) { Value = ci }
        );
    }
    public DataTable BuscarPadronGlobal(string texto)
    {
        return EjecutarPA(
            "PA_VOTANTE_BUSCAR_PADRON_GLOBAL",
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = texto }
        );
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
}