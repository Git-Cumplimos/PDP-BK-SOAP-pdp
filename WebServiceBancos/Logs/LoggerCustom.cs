using Amazon.Runtime.Internal.Transform;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
using WebServiceBancos.templates_lib;
using WebServiceBancos.ConexionBD;

namespace WebServiceBancos.Logs
{
    public class LoggerCustom
    {
        private DateTime crearFechaUtc_m5()
        {
            DateTime timeUtc = DateTime.UtcNow;
            try
            {
                TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);
            }
            catch (TimeZoneNotFoundException)
            {
                //Console.WriteLine("The registry does not define the Central Standard Time zone.");
                return timeUtc;
            }
            catch (InvalidTimeZoneException)
            {
                //Console.WriteLine("Registry data on the Central Standard Time zone has been corrupted.");
                return timeUtc;
            }
        }

        private OperationContext context;
        public DateTime fecha_start;
        public Dictionary<string, int> pk_log_type= new Dictionary<string, int>();
        public long id_log= 0;
        Utils obj_utils = new Utils();
        public LoggerCustom(OperationContext context_, DateTime ? optional_fecha_start = null)
        {
            pk_log_type.Add("SOAP", 1);
            context = context_;
            if (optional_fecha_start.HasValue == false)
            {
                fecha_start = crearFechaUtc_m5();
            }
            else
            {
                fecha_start = (DateTime)optional_fecha_start;
            }
        }
        
        public int get_pk_log_type_value(string key)
        {
            if (pk_log_type.ContainsKey(key) != true)
            {
                return 1;
            }
            else
            {
                int value;
                pk_log_type.TryGetValue(key, out value);
                return value;
            }
        }

        public void set_pk_log_type_value(string key, int value)
        {
            if (pk_log_type.ContainsKey(key) != true)
            {
                pk_log_type.Add(key, value);
            }
            else
            {
                pk_log_type[key]= value;
            }
        }

        public void before_app_request()
        {
            string requestXML = "";
            string endpoint = "";

            if (context != null && context.RequestContext != null)
            {
                var request = context.RequestContext.RequestMessage;
                endpoint = request.Headers.Action;
                requestXML = request.ToString();
            }

            
            Dictionary<string, dynamic> column_input = new Dictionary<string, dynamic>()
            {
                { "requestXML", requestXML}
            };

            int cant_soap= get_pk_log_type_value("SOAP");

            Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>()
            {
                { "pk_log_type", $"A_SOAP_{cant_soap}"},
                { "method", "SOAP"},
                { "endpoint", endpoint},
                { "input", obj_utils.JsonSerializerCustom(column_input)},
                { "time_start", fecha_start}
            };

            try
            {
                queries_base obj_queries_base = new queries_base();
                Dictionary<string, dynamic> res_insertar= obj_queries_base.Insertar_global("tbl_logs_recaudo_empresarial_pdp", data_insert);

                dynamic res_id_log = 0;
                res_insertar.TryGetValue("pk_log_servicio_recaudo_empresarial_pdp", out res_id_log);
                id_log = Convert.ToInt32(res_id_log);
                cant_soap += 1;
                set_pk_log_type_value("SOAP", cant_soap);
            }
            catch (ConexionBD.Conexion.NpgsqConnectionCustomException ex)
            {
                throw new Exception($"NpgsqConnectionCustomException = {ex.Message}");
            }
            catch (NpgsqlException ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
        }

        public void after_app_request(string root, Dictionary<string, dynamic> response, bool status, Dictionary<string, dynamic> additional = null, string id_trx = null)
        {
            XElement responseXML = new XElement(root,
                from keyValue in response
                select new XElement(keyValue.Key, keyValue.Value)
            );
            
            int cant_soap_previous = get_pk_log_type_value("SOAP")-1;
            Dictionary<string, dynamic> data_where = new Dictionary<string, dynamic>()
            {
                {"pk_log_servicio_recaudo_empresarial_pdp", id_log},
                {"pk_log_type", $"A_SOAP_{cant_soap_previous}" }
            };

            Dictionary<string, dynamic> column_output = new Dictionary<string, dynamic>()
            {
                {"responseXML", $"{responseXML}"}
            };

            if (additional != null  && additional.Count > 0)
            {
                column_output.Add("additional", obj_utils.JsonSerializerCustom(additional));
            }

            Dictionary<string, dynamic> data_set = new Dictionary<string, dynamic>()
            {
                {"time_end", crearFechaUtc_m5()},
                {"output", obj_utils.JsonSerializerCustom(column_output) },
                {"status",  status}
            };
            if (id_trx != null)
            {
                data_set.Add("fk_id_trx", id_trx);
            }
            var p = data_set;
            try { 
                queries_base obj_queries_base = new queries_base();
                obj_queries_base.Modificar("tbl_logs_recaudo_empresarial_pdp", data_set, data_where);
            }
            catch (ConexionBD.Conexion.NpgsqConnectionCustomException ex)
            {
                throw new Exception($"NpgsqConnectionCustomException = {ex.Message}");
            }
            catch (NpgsqlException ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
        }

        public void before_app_request_service(dynamic dataContract = null, System.Type typeDataContract = null)
        {
            string requestXML = "";
            string endpoint = "";

            if (context != null && context.RequestContext != null)
            {
                var request = context.RequestContext.RequestMessage;
                endpoint = request.Headers.Action;
                requestXML = request.ToString();
            }

            Dictionary<string, dynamic> column_input = new Dictionary<string, dynamic>()
            {
                { "requestXML", requestXML}
            };

            if (dataContract != null && typeDataContract != null)
            {
                //json
                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeDataContract);
                var stream_json = new MemoryStream();
                dataContractJsonSerializer.WriteObject(stream_json, dataContract);
                stream_json.Position = 0;
                StreamReader sr_json = new StreamReader(stream_json);
                string json_string = sr_json.ReadToEnd();
                column_input.Add("requestJSON", json_string);
            }

            int cant_soap = get_pk_log_type_value("SOAP");

            Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>()
            {
                { "pk_log_type", $"A_SOAP_{cant_soap}"},
                { "method", "SOAP"},
                { "endpoint", endpoint},
                { "input", obj_utils.JsonSerializerCustom(column_input)},
                { "time_start", fecha_start}
            };

            try
            {
                queries_base obj_queries_base = new queries_base();
                Dictionary<string, dynamic> res_insertar = obj_queries_base.Insertar_global("tbl_logs_recaudo_empresarial_pdp", data_insert);

                dynamic res_id_log = 0;
                res_insertar.TryGetValue("pk_log_servicio_recaudo_empresarial_pdp", out res_id_log);
                id_log = Convert.ToInt32(res_id_log);
                cant_soap += 1;
                set_pk_log_type_value("SOAP", cant_soap);
            }
            catch (ConexionBD.Conexion.NpgsqConnectionCustomException ex)
            {
                throw new Exception($"NpgsqConnectionCustomException = {ex.Message}");
            }
            catch (NpgsqlException ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
        }
        
        public void after_app_request_service(dynamic dataContract, System.Type typeDataContract, bool status, Dictionary<string, dynamic> additional = null, string id_trx = null)
        {
            //xml
            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeDataContract);
            MemoryStream stream_xml = new MemoryStream();
            dataContractSerializer.WriteObject(stream_xml, dataContract);
            stream_xml.Position = 0;
            StreamReader sr_xml = new StreamReader(stream_xml);
            string xml_string = sr_xml.ReadToEnd();

            //json
            DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeDataContract);
            var stream_json = new MemoryStream();
            dataContractJsonSerializer.WriteObject(stream_json, dataContract);
            stream_json.Position = 0;
            StreamReader sr_json = new StreamReader(stream_json);
            string json_string = sr_json.ReadToEnd();

            
            int cant_soap_previous = get_pk_log_type_value("SOAP") - 1;
            Dictionary<string, dynamic> data_where = new Dictionary<string, dynamic>()
            {
                {"pk_log_servicio_recaudo_empresarial_pdp", id_log},
                {"pk_log_type", $"A_SOAP_{cant_soap_previous}" }
            };

            Dictionary<string, dynamic> column_output = new Dictionary<string, dynamic>()
            {
                {"responseXML", xml_string},
                {"responseJSON", json_string}
            };

           
            if (additional != null && additional.Count > 0)
            {
                column_output.Add("additional", obj_utils.JsonSerializerCustom(additional));
            }

            Dictionary<string, dynamic> data_set = new Dictionary<string, dynamic>()
            {
                {"time_end", crearFechaUtc_m5()},
                {"output", obj_utils.JsonSerializerCustom(column_output) },
                {"status",  status}
            };
            if (id_trx != null)
            {
                data_set.Add("fk_id_trx", id_trx);
            }
            try
            {
                queries_base obj_queries_base = new queries_base();
                obj_queries_base.Modificar("tbl_logs_recaudo_empresarial_pdp", data_set, data_where);
            }
            catch (ConexionBD.Conexion.NpgsqConnectionCustomException ex)
            {
                throw new Exception($"NpgsqConnectionCustomException = {ex.Message}");
            }
            catch (NpgsqlException ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"NpgsqlException = {ex.Message}");
            }
            
        }
            
    }
}