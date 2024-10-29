using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

using WebServiceBancos.Aws;
using WebServiceBancos.Logs;
using System.Drawing;


namespace WebServiceBancos.templates_lib
{
    public class handle_sqs_utils
    {
        private LoggerCustom obj_logger_custom_sqs;
        public handle_sqs_utils(LoggerCustom OptionalLoggerCustom = null)
        {
            obj_logger_custom_sqs = OptionalLoggerCustom;
        }


        public string handle_recaudo_empresarial_pdp_sqs(
            short proceso,
            long id_comercio,
            string banco,
            decimal valor_total_trx,
            Dictionary<string, dynamic> data_contingencia,
            bool is_trx_contingencia,
            string application,
            string fecha_trx_asincrona,
            long id_log = 0,
            string id_trx = null
        )
        {
            Dictionary<string, dynamic> msg_dict = new Dictionary<string, dynamic>()
            {
                {"proceso", proceso},
                {"id_comercio", id_comercio },
                {"banco", banco },
                {"valor_total_trx", valor_total_trx},
                {"data_contingencia", data_contingencia},
                {"is_trx_contingencia", is_trx_contingencia},
                {"fecha_trx_asincrona", fecha_trx_asincrona},
                {"application", application},
                {"id_log", id_log}
            };
            if (id_trx != null)
            {
                msg_dict["id_trx"] = int.Parse(id_trx);
            }
            var y = JsonSerializer.Serialize(msg_dict);

            string sqsUrl = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("URL_SQS"));
            Sqs queue_handler = new Sqs(obj_logger_custom_sqs);

            string id_sqs = queue_handler.SendMessage(JsonSerializer.Serialize(msg_dict), sqsUrl);
            return id_sqs;
        }
    }

    public class trx_service
    {
        private LoggerCustom obj_logger_custom_trx;
        public trx_service(LoggerCustom OptionalLoggerCustom = null)
        {
            obj_logger_custom_trx = OptionalLoggerCustom;
        }

        public async Task<string> RealizarPeticionPost(
            object res,
            decimal valor,
            string tipo_operacion,
            long id_comercio,
            string id_terminal,
            string id_usuario,
            string nombre_usuario,
            string application
        )
        {
            application = application.ToUpper();
            string trx_url = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("URL_TRX_SERVICE_" + application));
            Dictionary<string, dynamic> msg_dict = new Dictionary<string, dynamic>()
                {
                     {"id_tipo_transaccion", tipo_operacion},
                     {"id_comercio", id_comercio},
                     {"id_usuario", id_usuario},
                     {"id_terminal", id_terminal},
                     {"Response", res},
                     {"Monto", valor},
                     {"nombre_usuario", nombre_usuario},
                };
            
            http queue_handler = new http(obj_logger_custom_trx);

            Dictionary<string, dynamic> resp_obj_http = await queue_handler.peticionHttp(msg_dict, $"{trx_url}/transaciones", "POST"); 
            
            dynamic obj = resp_obj_http["obj"];
            Dictionary<string, dynamic> obj2 = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(obj);
            string insertedId = obj2["inserted_id"].ToString();
            return insertedId;
        }

        public async Task<string> RealizarPeticionPost(
            object res,
            decimal valor,
            string tipo_operacion,
            long id_comercio,
            string id_terminal,
            string id_usuario,
            string nombre_usuario,
            string application,
            long id_trx
        )
        {
            string trx_url = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("URL_TRX_SERVICE_" + application));
            Dictionary<string, dynamic> msg_dict = new Dictionary<string, dynamic>()
                {                    
                    {"id_trx",id_trx},
                    {"id_tipo_transaccion", tipo_operacion},
                    {"id_comercio", id_comercio},
                    {"id_usuario", id_usuario},
                    {"id_terminal", id_terminal},
                    {"Response", res},
                    {"Monto", valor},
                    {"nombre_usuario", nombre_usuario},
                };
            http queue_handler = new http(obj_logger_custom_trx);

            Dictionary<string, dynamic> resp_obj_http = await queue_handler.peticionHttp(msg_dict, $"{trx_url}/transaciones", "POST",id_trx.ToString());

            dynamic obj = resp_obj_http["obj"];
            Dictionary<string, dynamic> obj2 = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(obj);
            string insertedId = obj2["inserted_id"].ToString();
            return insertedId;
        }

        public async Task RealizarPeticionPut(
            string id_trx,
            string tipo_operacion,
            string message_trx,
            object res,
            string application,
            bool status = false
        )
        {
            string trx_url = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("URL_TRX_SERVICE_" + application));
            if (status)
            {
                Dictionary<string, dynamic> msg_dict = new Dictionary<string, dynamic>()
                    {
                        {"message_trx", message_trx},
                        {"res_obj", res},
                        {"status_trx", status},
                        {"code_trx", 200}
                    };
                http queue_handler = new http(obj_logger_custom_trx);
                object resp_obj_http = await queue_handler.peticionHttp(msg_dict, $"{trx_url}/transaciones?id_trx={id_trx}&Tipo_operacion={tipo_operacion}", "PUT", id_trx);
            }
            else { 
                Dictionary<string, dynamic> msg_dict = new Dictionary<string, dynamic>()
                    {
                        {"message_trx", message_trx},
                        {"res_obj", res},
                        {"status_trx", status}
                    };
            http queue_handler = new http(obj_logger_custom_trx);
            object resp_obj_http = await queue_handler.peticionHttp(msg_dict, $"{trx_url}/transaciones?id_trx={id_trx}&Tipo_operacion={tipo_operacion}","PUT", id_trx);
            }
        }

        public async Task<List<dynamic>> RealizarPeticionGet(
            string id_trx,
            string tipo_operacion,
            string application
        )
        {
            string trx_url = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("URL_TRX_SERVICE_" + application));
            Dictionary<string, dynamic> msg_dict = new Dictionary<string, dynamic>() {};
            http queue_handler = new http(obj_logger_custom_trx);
            Dictionary<string, dynamic> resp_obj_http = await queue_handler.peticionHttp(msg_dict, $"{trx_url}/transaciones?id_trx={id_trx}&id_tipo_transaccion={tipo_operacion}", "GET", id_trx);
            dynamic list = JsonSerializer.Deserialize<List<dynamic>>(resp_obj_http["obj"]); 
            return list;
        }

    }

}