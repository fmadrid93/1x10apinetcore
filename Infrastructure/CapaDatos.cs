//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Data;
//using Microsoft.Data.SqlClient;

//namespace Infraestructure
//{
//    internal class CapaDatos:Conexion
//    {

//        //Procedimiento generico de registro de Bitacora para todas las entidades       
//        public bool RegistrarBitacora(Conexion cn, SqlTransaction st,
//            ref string IDBitacoraO,
//            string IDEntidad,
//            string IDUsuario,
//            string Accion,
//            string Glosa,
//            string IDSession

//        )
//        {

//            bool resultado = false;
//            SqlCommand SqlCmd;
//            try
//            {
//                SqlCmd = new SqlCommand("PA_SEG_InsBitacora", cn.obtenerConexion(), st);
//                SqlCmd.CommandType = CommandType.StoredProcedure;
//                SqlCmd.CommandTimeout = 999999;
//                SqlCmd.Parameters.Add("@IDEntidad", SqlDbType.VarChar, 50).Value = IDEntidad;
//                SqlCmd.Parameters.Add("@IDUsuario", SqlDbType.VarChar, 50).Value = IDUsuario;
//                SqlCmd.Parameters.Add("@Accion", SqlDbType.VarChar, 50).Value = Accion;
//                SqlCmd.Parameters.Add("@Glosa", SqlDbType.VarChar, 8000).Value = Glosa;
//                SqlCmd.Parameters.Add("@IDSession", SqlDbType.VarChar, 50).Value = IDSession;
//                SqlCmd.Parameters.Add("@IDBitacora", SqlDbType.UniqueIdentifier);
//                SqlCmd.Parameters["@IDBitacora"].Direction = ParameterDirection.Output;

//                SqlCmd.ExecuteNonQuery();
//                IDBitacoraO = Convert.ToString(SqlCmd.Parameters["@IDBitacora"].Value);

//                SqlCmd.Dispose();

//                resultado = true;

//            }
//            catch (Exception ex)
//            {
//                throw ex;

//            }
//            return resultado;
//        }
//        public bool RegistrarBitacoraInsert(
//           ref string IDBitacoraInsert,
//           string IDEntidad,
//           string IDUsuario,
//           string Accion,
//           string Glosa,
//           string IDSession

//       )
//        {

//            bool resultado = false;
//            SqlCommand SqlCmd;
//            try
//            {
//                abrirConexion();
//                SqlCmd = new SqlCommand("PA_SEG_InsBitacora", obtenerConexion());
//                SqlCmd.CommandType = CommandType.StoredProcedure;
//                SqlCmd.CommandTimeout = 999999;
//                SqlCmd.Parameters.Add("@IDEntidad", SqlDbType.VarChar, 50).Value = IDEntidad;
//                SqlCmd.Parameters.Add("@IDUsuario", SqlDbType.VarChar, 50).Value = IDUsuario;
//                SqlCmd.Parameters.Add("@Accion", SqlDbType.VarChar, 50).Value = Accion;
//                SqlCmd.Parameters.Add("@Glosa", SqlDbType.VarChar, 8000).Value = Glosa;
//                SqlCmd.Parameters.Add("@IDSession", SqlDbType.VarChar, 50).Value = IDSession;
//                SqlCmd.Parameters.Add("@IDBitacora", SqlDbType.UniqueIdentifier);
//                SqlCmd.Parameters["@IDBitacora"].Direction = ParameterDirection.Output;

//                SqlCmd.ExecuteNonQuery();
//                IDBitacoraInsert = Convert.ToString(SqlCmd.Parameters["@IDBitacora"].Value);
//                if (IDBitacoraInsert.Length > 5)
//                {
//                    SqlCmd.Dispose();

//                    resultado = true;
//                }
//            }
//            catch (Exception ex)
//            {
//                resultado = false;
//                cerrarConexion();
//                throw new Exception(ex.Message);

//            }
//            cerrarConexion();
//            return resultado;
//        }
//    }
//}
