using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Npgsql;
using Amazon.Runtime;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Configuration;
using System.Data.SqlClient;
using System.Runtime.InteropServices; //Sqs([Optional] LoggerCustom Optional_obj_logger_)
using System.Linq.Expressions;

using WebServiceBancos.Logs; 
using WebServiceBancos.templates_lib;
using WebServiceBancos.ConexionBD;
using System.Drawing;

using System.Text;
using System.Text.Json;
using System.Net.Http;



namespace WebServiceBancos.Aws
{

    public class Sqs
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

        Utils obj_utils = new Utils();
        private bool is_logger = false;
        private LoggerCustom obj_logger;
        private static string accessKey = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("ACCESS_KEY"));
        private static string secret = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("SECRET_KEY"));
        private static BasicAWSCredentials creds = new BasicAWSCredentials(accessKey, secret);
        private static IAmazonSQS sqsClient = new AmazonSQSClient(region: RegionEndpoint.USEast2);
        //private static IAmazonSQS sqsClient = new AmazonSQSClient(creds, RegionEndpoint.USEast2); local

        [Serializable]
        public class RequestSqsLoggerCustomException : Exception
        {
            public string Message_user = "";
            public RequestSqsLoggerCustomException(string message, Exception inner, string Message_user_)
                : base(message, inner) {
                Message_user = Message_user_;
            }
                
        }

        [Serializable]
        public class ResponseSqsLoggerCustomException : Exception
        {
            public string Message_user = "";
            public ResponseSqsLoggerCustomException(string message, Exception inner, string Message_user_ = "")
                : base(message, inner) {
                Message_user = Message_user_;
            }
        }

        public Sqs(LoggerCustom Optional_obj_logger_ = null)
        {
            is_logger = false;
            if (Optional_obj_logger_ != null)
            {
                is_logger = true;
                obj_logger = Optional_obj_logger_;
            }
        }

        public void sqs_request_logger(string Msg, string sqsUrl)
        {
            try
            {
                if (is_logger == true)
                {
                    try
                    {
                        int cant_sqs = obj_logger.get_pk_log_type_value("SQS");

                        string[] sqs_url_array = sqsUrl.Split('/');
                        Dictionary<string, dynamic> request_sqs = new Dictionary<string, dynamic>()
                        {
                            { "msg", Msg}
                        };
                        Dictionary<string, dynamic> column_input = new Dictionary<string, dynamic>()
                        {
                            {"requestSQS", obj_utils.JsonSerializerCustom(request_sqs)}
                        };
                        Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>()
                        {
                            { "pk_log_servicio_recaudo_empresarial_pdp", obj_logger.id_log},
                            { "pk_log_type", $"A_SQS_{cant_sqs}"},
                            { "method", "SQS"},
                            { "endpoint", $"SQS: {sqs_url_array[sqs_url_array.Length-1]}"},
                            { "input", obj_utils.JsonSerializerCustom(column_input)},
                            { "time_start", crearFechaUtc_m5()}
                        };

                        queries_base obj_queries_base = new queries_base();
                        Dictionary<string, dynamic> res_insertar = obj_queries_base.Insertar_global("tbl_logs_recaudo_empresarial_pdp", data_insert);
                        cant_sqs += 1;
                        obj_logger.set_pk_log_type_value("SQS", cant_sqs);
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
            catch (Exception ex)
            {
                throw new RequestSqsLoggerCustomException("Error con sqs_request_logger", ex, "Error con el registro de logger request sqs");
            }
        }

        public void sqs_response_logger(string MessageId, bool status)
        {
            try
            {
                if (is_logger == true)
                {
                    try
                    {
                        int cant_sqs_previous = obj_logger.get_pk_log_type_value("SQS") - 1;

                        Dictionary<string, dynamic> data_where = new Dictionary<string, dynamic>()
                        {
                            { "pk_log_servicio_recaudo_empresarial_pdp", obj_logger.id_log },
                            { "pk_log_type", $"A_SQS_{cant_sqs_previous}"}
                        };
                        Dictionary<string, dynamic> response_sqs = new Dictionary<string, dynamic>()
                        {
                            { "MessageId", MessageId},
                            { "status", status}
                        };
                        Dictionary<string, dynamic> column_output = new Dictionary<string, dynamic>()
                        {
                            { "responseSQS", obj_utils.JsonSerializerCustom(response_sqs)}
                        };
                        Dictionary<string, dynamic> data_set = new Dictionary<string, dynamic>()
                        {
                            { "output", obj_utils.JsonSerializerCustom(column_output) },
                            { "time_end", crearFechaUtc_m5() },
                            { "status", status }
                        };

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
            catch (Exception ex)
            {
                throw new ResponseSqsLoggerCustomException($"{ex.Message}", ex, "Error con el registro del logger response sqs");
            }
        }

        public  string SendMessage(string Msg, string sqsUrl) 
        {
            try
            {
                sqs_request_logger(Msg, sqsUrl);
                SendMessageResponse responseSendMsg = sqsClient.SendMessage(sqsUrl, Msg);
                string MessageId = responseSendMsg.MessageId;
                sqs_response_logger(MessageId, true);
                return MessageId;
            }
            catch(SqlException ex){
                sqs_response_logger("", false);
                throw ex;
            }
            catch (RequestSqsLoggerCustomException ex)
            {
                sqs_response_logger("", false);
                throw new RequestSqsLoggerCustomException($"RequestSqsLoggerCustomException = {ex.Message}",ex, ex.Message_user);
            }
            catch (ResponseSqsLoggerCustomException ex)
            {
                sqs_response_logger("", false);
                throw new ResponseSqsLoggerCustomException($"ResponseSqsLoggerCustomException = {ex.Message}", ex, ex.Message_user);
            }
            catch (Exception ex)
            {
                sqs_response_logger("", false);
                throw ex;
            }
        }
    }

    public class http
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

        Utils obj_utils = new Utils();
        private bool is_logger = false;
        private LoggerCustom obj_logger;


        [Serializable]
        public class RequestHttpLoggerCustomException : Exception
        {
            public string Message_user = "";
            public RequestHttpLoggerCustomException(string message, Exception inner, string Message_user_)
                : base(message, inner)
            {
                Message_user = Message_user_;
            }

        }

        [Serializable]
        public class ResponseHttpLoggerCustomException : Exception
        {
            public string Message_user = "";
            public ResponseHttpLoggerCustomException(string message, Exception inner, string Message_user_ = "")
                : base(message, inner)
            {
                Message_user = Message_user_;
            }
        }

        public http(LoggerCustom Optional_obj_logger_ = null)
        {
            is_logger = false;
            if (Optional_obj_logger_ != null)
            {
                is_logger = true;
                obj_logger = Optional_obj_logger_;
            }
        }

        public void http_request_logger(string Msg, string trx_url)
        {
            try
            {
                if (is_logger == true)
                {
                    try
                    {
                        int cant_http = obj_logger.get_pk_log_type_value("HTTP");

                        string[] trx_url_array = trx_url.Split('/');
                        Dictionary<string, dynamic> request_http = new Dictionary<string, dynamic>()
                        {
                            { "msg", Msg}
                        };
                        Dictionary<string, dynamic> column_input = new Dictionary<string, dynamic>()
                        {
                            {"requestHTTP", obj_utils.JsonSerializerCustom(request_http)}
                        };
                        Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>()
                        {
                            { "pk_log_servicio_recaudo_empresarial_pdp", obj_logger.id_log},
                            { "pk_log_type", $"A_HTTP_{cant_http}"},
                            { "method", "HTTP"},
                            { "endpoint", $"HTTP: {trx_url_array[trx_url_array.Length-1]}"},
                            { "input", obj_utils.JsonSerializerCustom(column_input)},
                            { "time_start", crearFechaUtc_m5()}
                        };

                        queries_base obj_queries_base = new queries_base();
                        Dictionary<string, dynamic> res_insertar = obj_queries_base.Insertar_global("tbl_logs_recaudo_empresarial_pdp", data_insert);
                        cant_http += 1;
                        obj_logger.set_pk_log_type_value("HTTP", cant_http);
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
            catch (Exception ex)
            {
                throw new RequestHttpLoggerCustomException("Error con http_request_logger", ex, "Error con el registro de logger request http");
            }
        }

        public void http_response_logger(string MessageId, bool status, string id_trx = null)
        {
            try
            {
                if (is_logger == true)
                {
                    try
                    {
                        int cant_http_previous = obj_logger.get_pk_log_type_value("HTTP") - 1;

                        Dictionary<string, dynamic> data_where = new Dictionary<string, dynamic>()
                        {
                            { "pk_log_servicio_recaudo_empresarial_pdp", obj_logger.id_log },
                            { "pk_log_type", $"A_HTTP_{cant_http_previous}"}
                        };
                        Dictionary<string, dynamic> response_http = new Dictionary<string, dynamic>()
                        {
                            { "MessageId", MessageId},
                            { "status", status}
                        };
                        Dictionary<string, dynamic> column_output = new Dictionary<string, dynamic>()
                        {
                            { "responseHTTP", obj_utils.JsonSerializerCustom(response_http)}
                        };
                        Dictionary<string, dynamic> data_set = new Dictionary<string, dynamic>()
                        {
                            { "output", obj_utils.JsonSerializerCustom(column_output) },
                            { "time_end", crearFechaUtc_m5() },
                            { "status", status }
                        };
                        if (id_trx != null)
                        {
                            data_set.Add("fk_id_trx", id_trx);
                        }

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
            catch (Exception ex)
            {
                throw new ResponseHttpLoggerCustomException($"{ex.Message}", ex, "Error con el registro del logger response sqs");
            }
        }

        public async Task<Dictionary<string, dynamic>> peticionHttp(object data, string trx_url,string tipo_peticion, string id_trx = null)
        {
            try
            {
                
                using (HttpClient client = new HttpClient())
                {

                    try
                    {
                        var datos = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                        http_request_logger(datos.ToString(), trx_url);
                        string contenido = "";
                        if (tipo_peticion == "POST")
                        { 
                            HttpResponseMessage response = await client.PostAsync(trx_url, datos);
                            contenido = await response.Content.ReadAsStringAsync();
                        }
                        else if (tipo_peticion == "PUT")
                        {
                            HttpResponseMessage response = await client.PutAsync(trx_url, datos);
                            contenido = await response.Content.ReadAsStringAsync();
                        }
                        else if (tipo_peticion == "GET")
                        {
                            HttpResponseMessage response = await client.GetAsync(trx_url);
                            contenido = await response.Content.ReadAsStringAsync();
                        }
                        Dictionary<string, dynamic> dict = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(contenido);
                        if (id_trx == null)
                        {
                            dynamic obj = dict["obj"];
                            Dictionary<string, dynamic> obj2 = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(obj);
                            id_trx = obj2["inserted_id"].ToString();
                        }
                        http_response_logger(dict.ToString(), true, id_trx);

                        return dict;
                        
                        
                        //return "";
                    }
                    catch (Exception excep)
                    { throw new Exception($"Exception = {excep.Message}", excep); }
                }
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            catch (RequestHttpLoggerCustomException ex)
            {
                throw new RequestHttpLoggerCustomException($"RequestHttpLoggerCustomException = {ex.Message}", ex, ex.Message_user);
            }
            catch (ResponseHttpLoggerCustomException ex)
            {
                throw new ResponseHttpLoggerCustomException($"ResponseHttpLoggerCustomException = {ex.Message}", ex, ex.Message_user);
            }
            catch (Exception excep)
            {
                throw new Exception($"{excep}");
            }
        }
    }
}

