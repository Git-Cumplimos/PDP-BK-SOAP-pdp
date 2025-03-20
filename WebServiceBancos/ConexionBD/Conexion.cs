using System.Linq;
using System.ServiceModel.Channels;
using System.Web;
using System.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Amazon.Runtime.Internal.Transform;
using System.Data;

namespace WebServiceBancos.ConexionBD
{
    public class Conexion
    {
        // PUNTO DE PAGO
        private static string DB_USER = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_USER"));
        private static string DB_PASSWORD = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_PASSWORD"));
        private static string DB_DBNAME = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_DBNAME"));
        private static string DB_HOST_READ = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_HOST_READ"));
        private static string DB_HOST_WRITE = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_HOST_WRITE"));
        private static string DB_PORT = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_PORT"));
        private static string cadenaConecctionRead = $"server={DB_HOST_READ}; port={DB_PORT}; user id={DB_USER}; password={DB_PASSWORD}; database={DB_DBNAME};";
        private static string cadenaConecctionWrite = $"server={DB_HOST_WRITE}; port={DB_PORT}; user id={DB_USER}; password={DB_PASSWORD}; database={DB_DBNAME};";

        //CONRED
        private static string DB_USER_CONRED = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_USER_CONRED"));
        private static string DB_PASSWORD_CONRED = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_PASSWORD_CONRED"));
        private static string DB_DBNAME_CONRED = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_DBNAME_CONRED"));
        private static string DB_HOST_READ_CONRED = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_HOST_READ_CONRED"));
        private static string DB_HOST_WRITE_CONRED = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_HOST_WRITE_CONRED"));
        private static string DB_PORT_CONRED = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("DB_PORT_CONRED"));
        private static string cadenaConecctionRead_CONRED = $"server={DB_HOST_READ_CONRED}; port={DB_PORT_CONRED}; user id={DB_USER_CONRED}; password={DB_PASSWORD_CONRED}; database={DB_DBNAME_CONRED};";
        private static string cadenaConecctionWrite_CONRED = $"server={DB_HOST_WRITE_CONRED}; port={DB_PORT_CONRED}; user id={DB_USER_CONRED}; password={DB_PASSWORD_CONRED}; database={DB_DBNAME_CONRED};";

        [Serializable]
        public class NpgsqConnectionCustomException : Exception 
        {
            public string Message_user = "";
            public NpgsqConnectionCustomException(string message, Exception inner,string message_user = "")
                : base(message, inner) {
                Message_user = message_user;
            }
        }

        protected dynamic input_data_convert_bd(dynamic value_)
        {
            if (value_ == null)
            {
                return "NULL";
            }
            if (value_.GetType() == typeof(DateTime))
            {
                var convertedDate = Convert.ToDateTime(value_);
                return $"'{convertedDate.ToString("yyyy/MM/dd HH:mm:ss")}'";
            }
            if (value_.GetType().Name.Contains("Dictionary") == true)
            {
                return $"'{JsonSerializer.Serialize(value_)}'";
            }
            return $"'{value_}'";
        }

        protected string input_data_convert_where_bd(string key_, dynamic value_)
        {
            if (value_.GetType() == typeof(string))
            {
                return $"{key_} LIKE '%{value_}%'";
            }
            else if (value_.GetType() == typeof(DateTime)) 
            {
                var convertedDate = Convert.ToDateTime(value_);
                string date_string= convertedDate.ToString("yyyy/MM/dd HH:mm:ss");
                return $"{key_} = '{date_string}'";
            }
            else
            {
                return $"{key_} = {value_}";
            }
        }

        protected dynamic ouput_data_convert_bd(NpgsqlDataReader ResultQuery, int position)
        {
            string type_column_bd = null;
            if (ResultQuery.IsDBNull(position) == false)
            {
                type_column_bd = ResultQuery.GetDataTypeName(position);
                switch (type_column_bd)
                {
                    case "bigint":
                        return ResultQuery.GetInt64(position);
                    case "timestamp without time zone":
                        return ResultQuery.GetDateTime(position);
                    case "json":
                        string data_string = ResultQuery.GetValue(position).ToString();
                        return JsonSerializer.Deserialize<Dictionary<string, dynamic>>(data_string);
                    case "boolean":
                        return ResultQuery.GetBoolean(position);
                    default:
                        if (type_column_bd.Contains("character varying") == true)
                        {
                            return ResultQuery.GetString(position);
                        }
                        break;
                }
            }
            else
            {
                return null;
            }

            return ResultQuery.GetValue(position);
        }

        protected Dictionary<string, dynamic> output_line_bd(NpgsqlDataReader ResultQuery)
        {
            ResultQuery.Read(); //para leer cada linea
            int cantColumn = ResultQuery.FieldCount;
            bool hasRows = ResultQuery.HasRows;
            Dictionary<string, dynamic> resultDiccionary = new Dictionary<string, dynamic>();
            if (hasRows)
            { 
                for (int i = 0; i < cantColumn; i++)
                {
                    resultDiccionary.Add(ResultQuery.GetName(i), ouput_data_convert_bd(ResultQuery,i));
                }
            }
            return resultDiccionary;
        }

        protected NpgsqlConnection open_connection(int db_ = 0)
        {
            NpgsqlConnection conex = new NpgsqlConnection();
            try
            {
                if (db_ == 1)
                {
                    conex.ConnectionString = cadenaConecctionRead;
                }
                else if (db_ == 2)
                {
                    conex.ConnectionString = cadenaConecctionWrite_CONRED;
                }
                else if (db_ == 3)
                {
                    conex.ConnectionString = cadenaConecctionRead_CONRED;
                }
                else
                {
                    conex.ConnectionString = cadenaConecctionWrite;
                }
                conex.Open();
            }
            catch (NpgsqlException ex)
            {
                throw new NpgsqConnectionCustomException(ex.Message, ex, "Error con la conexión a la base de datos");
            };

            return conex;
        }

        protected void close_connection(NpgsqlConnection conex)
        {
            conex.Close();
        }
    }
}