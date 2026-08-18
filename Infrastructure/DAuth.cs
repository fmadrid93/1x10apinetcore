using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infraestructure
{
    public class DAuth : Conexion
    {
        public bool VerificarUsuario(string Cuenta, string Contrasena, ref string tipoUsuario, ref string idUsuario, ref byte[] video, ref int version, ref string idGrupo, ref string color)
        {
            DataSet ds = new DataSet();
            bool resultado = false;
            try
            {
                abrirConexion();
                SqlCommand SqlCmd = new SqlCommand("PA_VerificarUsuario", obtenerConexion());
                SqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCmd.CommandTimeout = 999999;
                SqlCmd.Parameters.Add("@CUENTA", SqlDbType.VarChar, 50).Value = Cuenta;
                SqlCmd.Parameters.Add("@CONTRASENA", SqlDbType.VarChar, 150).Value = Contrasena;

                SqlCmd.Parameters.Add("@TIPOUSUARIO", SqlDbType.VarChar, 250);
                SqlCmd.Parameters["@TIPOUSUARIO"].Direction = ParameterDirection.Output;
                SqlCmd.Parameters.Add("@BIT", SqlDbType.Int);
                SqlCmd.Parameters["@BIT"].Direction = ParameterDirection.Output;
                SqlCmd.Parameters.Add("@IDUSUARIO", SqlDbType.VarChar, 50);
                SqlCmd.Parameters["@IDUSUARIO"].Direction = ParameterDirection.Output;
                SqlCmd.Parameters.Add("@Video", SqlDbType.VarBinary, -1);
                SqlCmd.Parameters["@Video"].Direction = ParameterDirection.Output;
                SqlCmd.Parameters.Add("@Version", SqlDbType.Int);
                SqlCmd.Parameters["@Version"].Direction = ParameterDirection.Output;

                SqlCmd.Parameters.Add("@IdGrupo", SqlDbType.VarChar, 50);
                SqlCmd.Parameters["@IdGrupo"].Direction = ParameterDirection.Output;
                SqlCmd.Parameters.Add("@Color", SqlDbType.VarChar, 50);
                SqlCmd.Parameters["@Color"].Direction = ParameterDirection.Output;
                SqlCmd.ExecuteNonQuery();
                tipoUsuario = SqlCmd.Parameters["@TIPOUSUARIO"].Value.ToString();
                idUsuario = SqlCmd.Parameters["@IDUSUARIO"].Value.ToString();
                idGrupo = SqlCmd.Parameters["@IdGrupo"].Value.ToString();
                color = SqlCmd.Parameters["@Color"].Value.ToString();
                if (SqlCmd.Parameters["@Video"].Value != null && SqlCmd.Parameters["@Video"].Value.ToString().Length >10)
                   video = (byte[])SqlCmd.Parameters["@Video"].Value;
                if (SqlCmd.Parameters["@Version"].Value != null  && SqlCmd.Parameters["@Version"].Value.ToString().Length >0)
                    version = int.Parse(SqlCmd.Parameters["@Version"].Value.ToString());
                SqlCmd.Dispose();


                //SqlDataAdapter sda = new SqlDataAdapter(SqlCmd);
                //sda.Fill(ds);
                resultado = true;
            }
            catch (Exception ex)
            {
                cerrarConexion();
                throw new Exception(ex.Message);
            }
            cerrarConexion();
            return resultado;
        }
        public DataSet ListarPreguntasEncuestaXUsuario(string idUsuario)
        {
            DataSet ds = new DataSet();
            try
            {
                abrirConexion();
                SqlCommand SqlCmd = new SqlCommand("PA_ListarPreguntasEncuestaXUsuario", obtenerConexion());

                SqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCmd.CommandTimeout = 999999;
                SqlCmd.Parameters.Add("@IdUsuario", SqlDbType.VarChar, 50).Value = idUsuario;
                SqlCmd.ExecuteNonQuery();
                SqlCmd.Dispose();


                SqlDataAdapter sda = new SqlDataAdapter(SqlCmd);
                sda.Fill(ds);
            }
            catch
            {
                cerrarConexion();
                throw new Exception("Ocurrio un error al Consultar el registro, verifique");
            }
            cerrarConexion();
            return ds;
        }
        public DataSet ListarPreguntasEncuestaXQR(string codigo)
        {
            DataSet ds = new DataSet();
            try
            {
                abrirConexion();
                SqlCommand SqlCmd = new SqlCommand("PA_ListarPreguntasEncuestaXQR", obtenerConexion());

                SqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCmd.CommandTimeout = 999999;
                SqlCmd.Parameters.Add("@Codigo", SqlDbType.VarChar, 50).Value = codigo;
                SqlCmd.ExecuteNonQuery();
                SqlCmd.Dispose();


                SqlDataAdapter sda = new SqlDataAdapter(SqlCmd);
                sda.Fill(ds);
            }
            catch
            {
                cerrarConexion();
                throw new Exception("Ocurrio un error al Consultar el registro, verifique");
            }
            cerrarConexion();
            return ds;
        }
        public DataSet ListarMarkersXUsuario( string idUsuario, DateTime fechaInicial, DateTime fechaFinal)
        {
            DataSet ds = new DataSet();
            try
            {

                abrirConexion();
                SqlCommand SqlCmd = new SqlCommand("PA_ListarMarkersXUsuario", obtenerConexion());
                SqlCmd.CommandType = CommandType.StoredProcedure;
                SqlCmd.CommandTimeout = 999999;
                SqlCmd.Parameters.Add("@IdUsuario", SqlDbType.VarChar, -1).Value = idUsuario;
                SqlCmd.Parameters.Add("@FechaInicial", SqlDbType.Date).Value = fechaInicial;
                SqlCmd.Parameters.Add("@FechaFinal", SqlDbType.Date).Value = fechaFinal;
                //  SqlCmd.Parameters.Add("@ConDato", SqlDbType.Bit).Value = conDato;
                SqlCmd.ExecuteNonQuery();
                SqlCmd.Dispose();


                SqlDataAdapter sda = new SqlDataAdapter(SqlCmd);
                sda.Fill(ds);
            }
            catch (Exception e)
            {
                cerrarConexion();
                throw new Exception("Ocurrio un error al Consultar el registro, verifique");
            }
            cerrarConexion();
            return ds;
        }

    }
}
