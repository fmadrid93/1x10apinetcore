using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infraestructure
{
    //internal class Conexion
    public class Conexion
    {
        SqlConnection cn;

        public Conexion()
        {

            string cnString = "Data Source = db.contactmanager.net,1433; Initial Catalog = AppCampana1x10; User id = ti; Password = elnoble2026;TrustServerCertificate=True";

            cn = new SqlConnection(cnString);
        }

        public void abrirConexion()
        {
            try
            {
                    if (cn.State == ConnectionState.Closed)
                    {
                        cn.Open();
                   }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al establecer conexión con el servidor de base de datos. " + ex.Message);
            }
        }

        public void cerrarConexion()
        {
            try
            {
                if (cn.State == ConnectionState.Open)
                {
                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cerrar la conexión con el servidor de base de datos. " + ex.Message);
            }
        }

        public SqlConnection obtenerConexion()
        {
            return cn;
        }

        public DataSet execSQLDataSet(Conexion cn, string sql)
        {
            try
            {
                DataSet ds = new DataSet();
                SqlCommand SqlCmd = new SqlCommand(sql, cn.obtenerConexion());
                SqlCmd.CommandType = CommandType.Text;
                SqlDataAdapter sda = new SqlDataAdapter(SqlCmd);
                sda.Fill(ds);
                return ds;
            }
            catch
            {
                throw new Exception("Error al ejecutar una sentencia SQL no válida.");
            }
        }

        public bool execSQLBool(Conexion cn, string sql)
        {
            bool resultado = false;
            try
            {
                DataSet ds = new DataSet();
                SqlCommand SqlCmd = new SqlCommand(sql, cn.obtenerConexion());
                SqlCmd.CommandType = CommandType.Text;
                SqlCmd.ExecuteNonQuery();
                resultado = true;
            }
            catch
            {
                throw new Exception("Error al ejecutar una sentencia SQL no válida.");
            }
            return resultado;
        }
    }
}
