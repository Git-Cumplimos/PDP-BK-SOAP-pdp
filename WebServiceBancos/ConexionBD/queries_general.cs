using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServiceBancos.ConexionBD
{
    public class queries_general : queries_base
    {
        public bool Consultar_tu()
        {


            //Conexion objConexion = new Conexion();
            NpgsqlConnection conn = open_connection();
            //string query = "select count(*) from tbl_recaudo_integrado_bancos where pk_recaudo_bancos = 80";
            //NpgsqlCommand conector = new NpgsqlCommand(query, conn);
            //NpgsqlDataAdapter datos = new NpgsqlDataAdapter(conector);
            //var VerificarComercio = conector.ExecuteReader();
            //VerificarComercio.Read();
            //int dato = VerificarComercio.GetInt32(0);
            //DataTable tabla = new DataTable();
            /*datos.Fill(tabla);  tabla.Rows[0]["count"].ToString()*/
            //if (dato == 1)
            // { 

            // return true;
            //}
            //else
            //{
            //return false;
            //}
            return true;




            /* Conexion objConexion = new Conexion();
             NpgsqlConnection conn = objConexion.establecerConexion();
             string query = "select * from tbl_recaudo_integrado_bancos where pk_recaudo_bancos = 7";
             NpgsqlCommand conector = new NpgsqlCommand(query, conn);
             var version = conector.ExecuteScalar().ToString();
             return version;*/

        }
    }
}