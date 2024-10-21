using Npgsql;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Text.Json;
using System.Data.Common;
using System.Security.Cryptography;
using System.Linq.Expressions;
using System.Web.Script.Serialization;
using Amazon.Runtime.Internal.Transform;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using System.Collections;
using System.Web.UI;
using Amazon.Auth.AccessControlPolicy;

namespace WebServiceBancos.ConexionBD
{

    public class queries_base : Conexion
    {
        public Dictionary<string, dynamic> Consultar_Paginacion(string tb_name, Dictionary<string, dynamic> data_search = null, string returning = "*")
        {
            int page = 1;
            int limit = 10;
            string sortDir = "ASC";
            string sortBy = null;
            string order_string = "";

            List<string> data_search_list = new List<string>();
            foreach (KeyValuePair<string, dynamic> data_ind in data_search)
            {
                if (data_search.ContainsKey("page") == true)
                {
                    page = data_search["page"];
                }
                else if (data_search.ContainsKey("limit") == true)
                {
                    limit = data_search["limit"];
                }
                else if (data_search.ContainsKey("sortDir") == true)
                {
                    sortDir = data_search["sortDir"];
                }
                else if (data_search.ContainsKey("sortBy") == true)
                {
                    sortBy = data_search["sortBy"];
                }
                else
                {
                    data_search_list.Add(item: input_data_convert_where_bd(data_ind.Key, data_ind.Value));
                }
            }

            string data_search_string = string.Join(", ", data_search_list);
            string where = "";
            if (data_search != null)
            {
                where = $"where {data_search_string}";
            }
            if (sortBy != null)
            {
                order_string = $"ORDER BY {sortBy} {sortDir}";
            }
            string pages_string = $"OFFSET (({page} - 1) * {limit}) ROWS FETCH NEXT {limit} ROWS ONLY";

            string query_count = $"select count(*) from {tb_name} {where};";
            int count = 0;
            using (NpgsqlConnection conn = open_connection(1))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query_count, conn))
                {
                    using (NpgsqlDataReader result_query_count = conector.ExecuteReader())
                    {
                        result_query_count.Read();
                        count = result_query_count.GetInt32(0);
                    }
                }

            }
          
            List<Dictionary<string, dynamic>> lista = new List<Dictionary<string, dynamic>>();
            string query = $"select {returning}  from {tb_name} {where};";
            using (NpgsqlConnection conn = open_connection(1))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader result_query = conector.ExecuteReader())
                    {
                        for (int row = 0; row < count; row++)
                        {
                            lista.Add(item: output_line_bd(result_query));
                        }
                    }
                }
            }

            decimal div = Convert.ToDecimal(count) / Convert.ToDecimal(limit);
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>()
            {
                {"results",lista},
                {"maxElems",count},
                {"maxPages", Math.Ceiling(div)},
                {"actualPage",page}
            };

            return result;
        }


        public List<Dictionary<string, dynamic>> Consultar(string tb_name, Dictionary<string, dynamic> data_search = null, string returning = "*")
        {
            List<string> data_search_list = new List<string>();
            foreach (KeyValuePair<string, dynamic> data_ind in data_search)
            {
                data_search_list.Add(item: input_data_convert_where_bd(data_ind.Key, data_ind.Value));
            }

            string data_search_string = string.Join(", ", data_search_list);
            string where = "";
            if (data_search != null)
            {
                where = $"where {data_search_string}";
            }

            string query_count = $"select count(*) from {tb_name} {where};";
            int count = 0;
            using (NpgsqlConnection conn = open_connection(1))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query_count, conn))
                {
                    using (NpgsqlDataReader result_query_count = conector.ExecuteReader())
                    {
                        result_query_count.Read();
                        count = result_query_count.GetInt32(0);
                    }
                }
            }

            List<Dictionary<string, dynamic>> lista = new List<Dictionary<string, dynamic>>();
            string query = $"select {returning}  from {tb_name} {where};";
            using (NpgsqlConnection conn = open_connection(1))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader result_query = conector.ExecuteReader())
                    {
                        for (int row=0; row<count; row++)
                        {
                            lista.Add(item:output_line_bd(result_query));
                        }
                    }
                }
            }

            return lista;
        }


        public Dictionary<string, dynamic> Insertar_global(string tb_name, Dictionary<string, dynamic> data_insert, string returning="*")
        {
            List<string> data_keys_list = new List<string>();
            List<dynamic> data_value_list = new List<dynamic>();
            foreach (KeyValuePair<string, dynamic> data_ind in data_insert)
            {
                data_keys_list.Add(item: data_ind.Key);
                data_value_list.Add(item: input_data_convert_bd(data_ind.Value));
            }
            string data_names = string.Join(", ", data_keys_list);
            string data_values = string.Join(", ", data_value_list);

            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            string query = $"INSERT INTO {tb_name} ({data_names}) VALUES ({data_values}) RETURNING {returning};";
            using (NpgsqlConnection conn = open_connection(0))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader ResultQuery = conector.ExecuteReader())
                    {
                        result = output_line_bd(ResultQuery);
                    }
                }
            }

            return result;
        }

         
        public Dictionary<string, dynamic> Modificar(string tb_name, Dictionary<string, dynamic> data_set, Dictionary<string, dynamic> data_where, bool insert_on_non_exiting = false, string returning = "*")
        {
            List<string> data_set_list = new List<string>();
            foreach (KeyValuePair<string, dynamic> data_ind in data_set)
            {
                data_set_list.Add(item: $"{data_ind.Key}= {input_data_convert_bd(data_ind.Value)}");
            }
            string data_set_string = string.Join(", ", data_set_list);

            List<dynamic> data_where_list = new List<dynamic>();
            foreach (KeyValuePair<string, dynamic> data_ind in data_where)
            {
               data_where_list.Add(item: input_data_convert_where_bd(data_ind.Key, data_ind.Value));
            }
            string data_where_string = string.Join(" and ", data_where_list);

            string query = $"UPDATE {tb_name} set {data_set_string} WHERE {data_where_string} RETURNING {returning};";
            
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            bool estado_query = false;
            using (NpgsqlConnection conn = open_connection(0))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader ResultQuery = conector.ExecuteReader())
                    {
                        estado_query = ResultQuery.HasRows; // para leer si hay un resultado
                        if (estado_query == true)
                        {
                            result = output_line_bd(ResultQuery);
                        }
                    }
                }
            }

            // para insertar el dato si no no se puede modificar y insert_on_non_exiting es true
            if (estado_query == false)
            {
                if (insert_on_non_exiting == true)
                {
                    Dictionary<string, dynamic> data_insert = data_set;
                    foreach (KeyValuePair<string, dynamic> data_ind in data_where)
                    {
                        data_insert.Add(data_ind.Key, data_ind.Value);
                    }
                    result = this.Insertar(tb_name, data_insert, returning);
                }
            }
            return result;
        }

        public Dictionary<string, dynamic> Insertar(string tb_name, Dictionary<string, dynamic> data_insert, string application, string returning = "*")
        {
            int basedatos = 0;
            if (application == "CONRED")
            {
                basedatos = 2;
            }
            List<string> data_keys_list = new List<string>();
            List<dynamic> data_value_list = new List<dynamic>();
            foreach (KeyValuePair<string, dynamic> data_ind in data_insert)
            {
                data_keys_list.Add(item: data_ind.Key);
                data_value_list.Add(item: input_data_convert_bd(data_ind.Value));
            }
            string data_names = string.Join(", ", data_keys_list);
            string data_values = string.Join(", ", data_value_list);

            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            string query = $"INSERT INTO {tb_name} ({data_names}) VALUES ({data_values}) RETURNING {returning};";
            using (NpgsqlConnection conn = open_connection(basedatos))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader ResultQuery = conector.ExecuteReader())
                    {
                        result = output_line_bd(ResultQuery);
                    }
                }
            }

            return result;
        }
        public List<Dictionary<string, dynamic>> ConsultarRecaudo(string tb_name, Dictionary<string, dynamic> data_search, string application, string returning = "*")
        {
            int basedatos = 1;
            if (application == "CONRED")
            {
                basedatos = 3;
            }
            var valor_trx = data_search["valor_trx"];
            data_search.Remove("valor_trx");

            //string jsonString = JsonSerializer.Serialize(data_search);
            // Serializar el diccionario a JSON
            string jsonString = JsonSerializer.Serialize(data_search, new JsonSerializerOptions { WriteIndented = false });

            // Manipular la cadena JSON para agregar un espacio después de los dos puntos
            string formattedJsonString = jsonString.Replace(":", ": ").Replace(",", ", ");
            List<Dictionary<string, dynamic>> lista = new List<Dictionary<string, dynamic>>();
            string query = $"select id_trx, status_trx, data_contingencia from {tb_name} WHERE valor_trx = {valor_trx} AND" +
                $" data_contingencia::jsonb @> '{formattedJsonString}';";
            Console.WriteLine(query);
            using (NpgsqlConnection conn = open_connection(basedatos))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader result_query = conector.ExecuteReader())
                    {
                        Dictionary<string, dynamic> out_ = output_line_bd(result_query);
                        if (out_.Count != 0)
                        {
                            lista.Add(out_);
                        }
                    }
                }
            }
            return lista;
        }
        private string crearQueryComercios(int id_comercio , bool conred = false)
        {
            List<string> list_columns = new List<string>() {
                "COALESCE(users.id_terminal, -1) AS id_terminal",
                "COALESCE(users.id_usuario_suser, -1) AS id_usuario_suser",
                "COALESCE(users.uname, 'SIN NOMBRE') AS nombre_usuario",
                "users.uuid",
            };

            List<string> list_condition = new List<string>() {
                "users.active = True ",
                $"users.fk_id_comercio = {id_comercio} "
            };

            if (!conred)
                list_condition.Add("NOT users.id_usuario_suser is null ");
            else {
                list_columns = new List<string>() {
                    "users.uuid AS id_terminal",
                    "users.uuid AS id_usuario_suser",
                    "users.uname AS nombre_usuario",
                };
            }

            string condition = String.Join(" AND ", list_condition);
            string extra_columns= String.Join(", ", list_columns);

            string query = $"SELECT " +
                $"    {extra_columns}" +
                " FROM public.users" +
                " INNER JOIN public.tbl_comercios as comer" +
                " ON users.fk_id_comercio = comer.pk_comercio" +
                " INNER JOIN tbl_union_grupo_comercios" +
                " ON tbl_union_grupo_comercios.fk_comercio = comer.pk_comercio" +
                " INNER JOIN tbl_grupo_comercios as groups" +
                " ON groups.pk_tbl_grupo_comercios = tbl_union_grupo_comercios.fk_tbl_grupo_comercios" +
                $" WHERE {condition}" +
                " ORDER BY users.fecha_actualizacion DESC LIMIT 1;";
            return query;
        }

        public (List<Dictionary<string, dynamic>>,string) ConsultarComercio(int id_comercio, string returning = "*")
        {
            string query = (string)crearQueryComercios(id_comercio, false);
            string query_conred = (string)crearQueryComercios(id_comercio, true);
            string application = "PDP";
            List<Dictionary<string, dynamic>> lista = new List<Dictionary<string, dynamic>>();
            using (NpgsqlConnection conn = open_connection(1))
            {
                using (NpgsqlCommand conector = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader result_query = conector.ExecuteReader())
                    {
                        Dictionary<string, dynamic> out_ = output_line_bd(result_query);
                        if (out_.Count != 0)
                        {
                            lista.Add(out_);
                        }
                        else
                        {
                            using (NpgsqlConnection conn_conred = open_connection(3))
                            {
                                using (NpgsqlCommand conector_conred = new NpgsqlCommand(query_conred, conn_conred))
                                {
                                    using (NpgsqlDataReader result_query_conred = conector_conred.ExecuteReader())
                                    {
                                        Dictionary<string, dynamic> out_conred = output_line_bd(result_query_conred);
                                        if (out_conred.Count != 0)
                                        {
                                            lista.Add(out_conred);
                                            application = "Conred";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (lista, application);
        }
    }
}