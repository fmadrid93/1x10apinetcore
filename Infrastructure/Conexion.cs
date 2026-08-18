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
            //   string cnString = "Data Source=localhost;Initial Catalog=EncuestaMadrid;User ID=sa;Password=Mo7oJob08o!684;TrustServerCertificate=True";
            // string cnString = "Data Source=54.234.108.196;Initial Catalog=EncuestaMadrid;User ID=sa;Password=Mo7oJob08o!684;TrustServerCertificate=True";
            //   string cnString = "Data Source=localhost,1433;Initial Catalog=EncuestaMadrid;User id=sa;Password=Mo7oJob08o!684;TrustServerCertificate=True";
            //  string cnString = "Data Source=sqlserver,1433;Initial Catalog=EncuestaMadrid;User ID=sa;Password=Mo7oJob08o!684;TrustServerCertificate=True";

            string cnString = "Data Source = db.contactmanager.net,1433; Initial Catalog = AppCampana1x10; User id = sa; Password = Y0m@drid2021;TrustServerCertificate=True";
          //  string cnString = "Data Source = 127.0.0.1; Initial Catalog = AppCampana1x10; User id = sa; Password = 12345;TrustServerCertificate=True";
          //  string cnString = "Data Source = localhost; Initial Catalog = EncuestaMadrid-beni; User id = sa; Password = 210018501;TrustServerCertificate=True";
            //  string cnString = "Data Source = localhost; Initial Catalog = EncuestaMadridDocker; User id = sa; Password = 210018501;TrustServerCertificate=True";

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
