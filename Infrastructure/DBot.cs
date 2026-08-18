using Infrastructure;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

public class DBot : DbHelper
{
    public DataTable ObtenerRecinto(string celular)
    {
        return EjecutarPA(
            "PA_ObtenerRecinto",
            new SqlParameter("@Celular", SqlDbType.VarChar, 50) { Value = celular }
        );
    }

}