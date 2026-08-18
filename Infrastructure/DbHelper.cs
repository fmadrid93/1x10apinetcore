using System;
using System.Data;
using Infraestructure;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DbHelper : Conexion
    {
        protected DataTable EjecutarPA(string nombrePA, params SqlParameter[] parametros)
        {
            DataSet ds = new DataSet();

            try
            {
                abrirConexion();

                using (SqlCommand cmd = new SqlCommand(nombrePA, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 999999;

                    if (parametros != null && parametros.Length > 0)
                    {
                        cmd.Parameters.AddRange(parametros);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                     
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ocurrio un error al ejecutar {nombrePA}: {ex.Message}");
            }
            finally
            {
                cerrarConexion();
            }

            return ds.Tables[0];
        
    }
      protected DataSet EjecutarPA_DS(string nombrePA, params SqlParameter[] parametros)
        {
            DataSet ds = new DataSet();

            try
            {
                abrirConexion();

                using (SqlCommand cmd = new SqlCommand(nombrePA, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 999999;

                    if (parametros != null && parametros.Length > 0)
                    {
                        cmd.Parameters.AddRange(parametros);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);

                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ocurrio un error al ejecutar {nombrePA}: {ex.Message}");
            }
            finally
            {
                cerrarConexion();
            }

            return ds;
        }
    }
}