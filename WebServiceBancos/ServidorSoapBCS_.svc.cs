using Npgsql;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Configuration;
using System.Text.Json;
using Amazon.Runtime.Internal.Transform;
using System.Data.SqlClient;
using System.ServiceModel.Description;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using Amazon.Runtime.Internal;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Web.UI.WebControls;
using System.Xml.Linq;

using WebServiceBancos.CServidorSoapBCS;
using WebServiceBancos.Logs;
using WebServiceBancos.ConexionBD;
using WebServiceBancos.templates_lib;
using System.Globalization;
using System.Web.Services.Protocols;
using System.Web.Services;

namespace WebServiceBancos
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "ServidorSoapBCS_" en el código, en svc y en el archivo de configuración a la vez.
    // NOTA: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione ServidorSoapBCS_.svc o ServidorSoapBCS_.svc.cs en el Explorador de soluciones e inicie la depuración.
    [ServiceBehavior(Name = "ServicioRecaudosBCSService")]
    public class ServidorSoapBCS_ : FServidorSoapBCS, IServidorSoapBCS_
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

        [OperationBehavior]
        public consultaRecaudoBCSResponse consultaRecaudoBCS(transactionXMLConsultaRequest input)
        {
            consultaRecaudoBCSResponse output = new consultaRecaudoBCSResponse();
            output.transactionXML = new transactionXMLResponse();
            output.transactionXML.parametersXML = new parametersXMLResponse();

            DataContractConsultaRecaudoBCSResponseExitosa Out_exitosa = new DataContractConsultaRecaudoBCSResponseExitosa();
            Out_exitosa.paymentReference = input.transactionXML.contentXML.consultOfCollectionRequest.reference1;
            Out_exitosa.EANCode = input.transactionXML.contentXML.consultOfCollectionRequest.EANCode;
            Out_exitosa.reference1 = input.transactionXML.contentXML.consultOfCollectionRequest.reference1;
            Out_exitosa.reference2 = "";
            Out_exitosa.expirationDate = input.transactionXML.contentXML.consultOfCollectionRequest.expirationDate;
            Out_exitosa.paymentDate = input.transactionXML.contentXML.consultOfCollectionRequest.transactionDate;
            Out_exitosa.paymentType = "0";

            DataContractConsultaRecaudoBCSResponse Out_fallida = new DataContractConsultaRecaudoBCSResponse();
            Out_fallida.paymentReference = input.transactionXML.contentXML.consultOfCollectionRequest.reference1;
            Out_fallida.EANCode = input.transactionXML.contentXML.consultOfCollectionRequest.EANCode;
            Out_fallida.reference1 = input.transactionXML.contentXML.consultOfCollectionRequest.reference1;
            Out_fallida.reference2 = "";
            Out_fallida.expirationDate = input.transactionXML.contentXML.consultOfCollectionRequest.expirationDate;
            Out_fallida.paymentDate = input.transactionXML.contentXML.consultOfCollectionRequest.transactionDate;
            Out_fallida.paymentType = "0";

            Dictionary<string, dynamic> data_flujo = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>();
            string application = "";
            string error_name = "";
            string error_user = "";
            string error_pdp = "";
            LoggerCustom obj_logger = new LoggerCustom(OperationContext.Current);

            //Secuencia log de entrada
            //<<<<<<>>>>>>>>>>
            error_name = "error_log_request";
            error_user = "Error con el log request";
            error_pdp = "Error respuesta PDP: (Error con los logs [consultaRecaudoBCS])";
            //<<<<<<>>>>>>>>>>
            try
            {
                obj_logger.before_app_request();
            }
            catch (Exception ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out_fallida.totalValue = 0;
                Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                Out_fallida.responseCode = "ER";
                Out_fallida.responseError.errorCode = "00001";
                Out_fallida.responseError.errorType = "GEN";
                Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
                return output;
            }

            //llamar al flujo
            Tuple<consultaRecaudoBCSResponse, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> res_flujo = FlujoConsultaRecaudoBCS(input, obj_logger);
            output = res_flujo.Item1;
            error_msg = res_flujo.Item2;
            string id_trx = res_flujo.Item3;
            data_flujo = res_flujo.Item4;
            application = (string)data_flujo["application"];
            application=application.ToUpper();
            string terminal = (string)data_flujo["id_terminal"];
            string usuario = (string)data_flujo["id_usuario"];
            string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_CONS_REC_EMP_" + application));
            long referencia1;
            string id_comercio = input.transactionXML.contentXML.consultOfCollectionRequest.reference1;
            if (!Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = -1;
            }
            else
            {
                referencia1 = Convert.ToInt64(input.transactionXML.contentXML.consultOfCollectionRequest.reference1);
            }
            decimal totalValue = 0;
            if (decimal.TryParse(input.transactionXML.contentXML.consultOfCollectionRequest.totalValue, out totalValue))
            {
                totalValue = (totalValue / 100);
            }
            DataContractConsultaRecaudoBCSResponseExitosa data = output.transactionXML.parametersXML.consultOfColectionResponse as DataContractConsultaRecaudoBCSResponseExitosa;
            DataContractConsultaRecaudoBCSResponse data_error = output.transactionXML.parametersXML.consultOfColectionResponse as DataContractConsultaRecaudoBCSResponse;

            if (data != null)
            {
                Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>(){
                            { "id_trx",(id_trx != "")?id_trx:null},
                            { "id_log_trx", obj_logger.id_log },
                            { "nombre_banco", "caja_social" },
                            { "valor_trx", totalValue.ToString().Replace(",", ".") },
                            { "fecha_trx", crearFechaUtc_m5() },
                            { "res_obj", new Dictionary<string, dynamic>(){
                                { "msg", "Caja social- consulta "+data.responseDescription},
                                { "status", data.responseCode.Equals("OK") },
                                { "codigo", data.responseCode },
                                { "obj", error_msg },
                            } },
                            { "status_trx", data.responseCode.Equals("OK") },
                            { "data_contingencia", null },
                            { "id_tipo_transaccion", type_trx_rec},
                            { "name_tipo_transaccion", "Caja Social - Consulta Recaudo" },
                            { "inf_comercio", new Dictionary<string, dynamic>(){
                                        {"id_comercio", (input.transactionXML.contentXML.consultOfCollectionRequest.reference1 != "") ? referencia1 : (long?)null},
                                        {"id_usuario", usuario},
                                        {"id_terminal", terminal}
                            }},
                            { "fecha_trx_asincrona", crearFechaUtc_m5()},
                            { "is_trx_contingencia", false}
                        };
                Console.WriteLine(data_insert);
                try
                {
                    queries_base query_helper = new queries_base();
                    query_helper.Insertar("tbl_recaudo_integrado_bancos", data_insert, application);
                }
                catch (ConexionBD.Conexion.NpgsqConnectionCustomException ex)
                {
                    error_user = ex.Message;
                    throw new Exception($"NpgsqConnectionCustomException = {ex}", ex);
                }
                catch (NpgsqlException ex)
                {
                    throw new Exception($"NpgsqlException = {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception = {ex.Message}", ex);
                }
                //Secuencia actualizar log id trx 
                //<<<<<<>>>>>>>>>>
                error_name = "error_log_response";
                error_user = "Error con al modificar el response en los logs id trx";
                error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
                //<<<<<<>>>>>>>>>>
                try
                {
                    trx_service RealizarPeticion = new trx_service(obj_logger);
                    RealizarPeticion.RealizarPeticionPut(data_insert["id_trx"], type_trx_rec, data_insert["res_obj"]["msg"], data_insert,application,data.responseCode.Equals("OK")).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    Out_fallida.responseCode = "ER";
                    Out_fallida.responseError.errorCode = "00001";
                    Out_fallida.responseError.errorType = "GEN";
                    Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
                    return output;
                }

                //<<<<<<>>>>>>>>>> Secuencia response log
                //<<<<<<>>>>>>>>>>
                error_name = "error_log_response";
                error_user = "Error con response log";
                error_pdp = "Error respuesta PDP: (Error con los logs [consultaRecaudoBCS])";
                //<<<<<<>>>>>>>>>>
                try
                {
                    Dictionary<string, dynamic> aditional = null;
                    if (error_msg["context"] != "")
                    {
                        aditional = new Dictionary<string, dynamic>() { { "causal", $"{error_msg["context"]}" } };
                    }
                    obj_logger.after_app_request_service(output, typeof(consultaRecaudoBCSResponse), true, aditional, data_insert["id_trx"]);
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", false},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                }
                return output;
            }
            else
            {
                Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>(){
                    { "id_trx",(id_trx != "")?id_trx:null},
                    { "id_log_trx", obj_logger.id_log },
                    { "nombre_banco", "caja_social" },
                    { "valor_trx", data_error.totalValue },
                    { "fecha_trx", crearFechaUtc_m5() },
                    { "res_obj", new Dictionary<string, dynamic>(){
                        { "msg", "Caja social- consulta "+data_error.responseError.errorDescription},
                        { "status", data_error.responseCode.Equals("OK") },
                        { "codigo", data_error.responseCode },
                        { "obj", error_msg },
                    } },
                    { "status_trx", data_error.responseCode.Equals("OK") },
                    { "data_contingencia", null },
                    { "id_tipo_transaccion", type_trx_rec },
                    { "name_tipo_transaccion", "Caja Social - Consulta Recaudo" },
                    { "inf_comercio", new Dictionary<string, dynamic>(){
                                {"id_comercio", (input.transactionXML.contentXML.consultOfCollectionRequest.reference1 != "") ? referencia1 : (long?)null},
                                {"id_usuario", usuario},
                                {"id_terminal", terminal}
                    }},
                    { "fecha_trx_asincrona", crearFechaUtc_m5()},
                    { "is_trx_contingencia", false}
                };
                Console.WriteLine(data_insert);
                try
                {
                    queries_base query_helper = new queries_base();
                    query_helper.Insertar("tbl_recaudo_integrado_bancos", data_insert,application);
                }
                catch (ConexionBD.Conexion.NpgsqConnectionCustomException ex)
                {
                    error_user = ex.Message;
                    throw new Exception($"NpgsqConnectionCustomException = {ex}", ex);
                }
                catch (NpgsqlException ex)
                {
                    throw new Exception($"NpgsqlException = {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception = {ex.Message}", ex);
                }
                //Secuencia actualizar log id trx 
                //<<<<<<>>>>>>>>>>
                error_name = "error_log_response";
                error_user = "Error con al modificar el response en los logs id trx";
                error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
                //<<<<<<>>>>>>>>>>
                try
                {
                    trx_service RealizarPeticion = new trx_service(obj_logger);
                    RealizarPeticion.RealizarPeticionPut(data_insert["id_trx"], type_trx_rec, data_insert["res_obj"]["msg"], data_insert, application,data_error.responseCode.Equals("OK")).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    Out_fallida.responseCode = "ER";
                    Out_fallida.responseError.errorCode = "00001";
                    Out_fallida.responseError.errorType = "GEN";
                    Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
                    return output;
                }
                //<<<<<<>>>>>>>>>> Secuencia response log
                //<<<<<<>>>>>>>>>>
                error_name = "error_log_response";
                error_user = "Error con response log";
                error_pdp = "Error respuesta PDP: (Error con los logs [consultaRecaudoBCS])";
                //<<<<<<>>>>>>>>>>
                try
                {
                    Dictionary<string, dynamic> aditional = null;
                    if (error_msg["context"] != "")
                    {
                        aditional = new Dictionary<string, dynamic>() { { "causal", $"{error_msg["context"]}" } };
                    }
                    obj_logger.after_app_request_service(output, typeof(consultaRecaudoBCSResponse), true, aditional, data_insert["id_trx"]);
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", false},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                }
                return output;
            }

        }

        [OperationBehavior]
        public notificacionRecaudoBCSResponse notificacionRecaudoBCS(transactionXMLNotificacionRequest input)
        {
            notificacionRecaudoBCSResponse output = new notificacionRecaudoBCSResponse();
            output.transactionXML = new transactionXMLNotResponse();
            output.transactionXML.parametersXML = new parametersXMLNotResponse();
            output.transactionXML.parametersXML.notificationOfCollectionResponse = new DataContractNotificacionRecaudoBCSResponse();

            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionDate = input.transactionXML.contentXML.notificationOfCollectionRequest.paymentDate;
            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionCode = input.transactionXML.contentXML.notificationOfCollectionRequest.transactionCode;

            Dictionary<string, dynamic> data_flujo = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>();
            string application = "";
            string error_name = "";
            string error_user = "";
            string error_pdp = "";
            LoggerCustom obj_logger = new LoggerCustom(OperationContext.Current);

            //Secuencia log de entrada
            //<<<<<<>>>>>>>>>>
            error_name = "error_log_request";
            error_user = "Error con el log request";
            error_pdp = "Error respuesta PDP: (Error con los logs [Notificación])";
            //<<<<<<>>>>>>>>>>
            try
            {
                obj_logger.before_app_request();
            }
            catch (Exception ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ER";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";           
                return output;
            }

            //llamar al flujo
            Tuple<notificacionRecaudoBCSResponse, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> res_flujo = FlujoNotificacionRecaudoBCS(input, obj_logger);
            output = res_flujo.Item1;
            error_msg = res_flujo.Item2;
            string id_trx = res_flujo.Item3;
            data_flujo = res_flujo.Item4;
            application = (string)data_flujo["application"];
            application = application.ToUpper();
            string terminal = (string)data_flujo["id_terminal"];
            string usuario = (string)data_flujo["id_usuario"];
            string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_REC_EMP_" + application));
            long referencia1 = -1;
            string id_comercio = input.transactionXML.contentXML.notificationOfCollectionRequest.reference1;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(input.transactionXML.contentXML.notificationOfCollectionRequest.reference1);
            }            
           
            DateTime fecha = DateTime.UtcNow.Subtract(TimeSpan.FromHours(5));
            decimal totalValue = 0;
            if (decimal.TryParse(input.transactionXML.contentXML.notificationOfCollectionRequest.totalValue, out totalValue))
            {
                totalValue = totalValue / 100;
            }

            //Secuencia registrar en tbl_recaudo_integrado_bancos
            //if(true) //PILAS QUITAR!!!
            if (output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode != "OK")
            {
                Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>(){
                        { "id_trx", (id_trx != "") ? id_trx : null },
                        { "id_log_trx", obj_logger.id_log },
                        { "nombre_banco", "caja_social" },
                        { "valor_trx", totalValue.ToString().Replace(",",".") },
                        { "fecha_trx",fecha},
                        { "res_obj", new Dictionary<string, dynamic>(){
                            { "msg", $"Caja social- notificación {output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription}" },
                            { "status", false },
                            { "codigo", 500},
                            { "obj", error_msg },
                        }},
                        { "status_trx", false },
                        { "data_contingencia", new Dictionary<string, dynamic>(){} },
                        { "id_tipo_transaccion", type_trx_rec },
                        { "name_tipo_transaccion", "Caja social - Notificación recaudo" },
                        { "inf_comercio", new Dictionary<string, dynamic>(){
                                    {"id_comercio",referencia1 },
                                    {"id_usuario", usuario},
                                    {"id_terminal", terminal}
                        }},
                        { "fecha_trx_asincrona", fecha},
                        { "is_trx_contingencia", false}
                    };
                Console.WriteLine(data_insert);
                try
                {
                    queries_base query_helper = new queries_base();
                    query_helper.Insertar("tbl_recaudo_integrado_bancos", data_insert,application);
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ER";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    return output;
                }

                //Secuencia actualizar registro id trx 
                //<<<<<<>>>>>>>>>>
                error_name = "error_log_response";
                error_user = "Error con al modificar el response en los logs id trx";
                error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
                //<<<<<<>>>>>>>>>>
                try
                {
                    trx_service RealizarPeticion = new trx_service(obj_logger);
                    RealizarPeticion.RealizarPeticionPut(id_trx, type_trx_rec, "Error notificación recaudo", data_insert, application, false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };

                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ER";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    return output;

                }
            }

            //Secuencia response log
            //<<<<<<>>>>>>>>>>
            error_name = "error_log_response";
            error_user = "Error con response log";
            error_pdp = "Error respuesta PDP: (Error con los logs)";
            //<<<<<<>>>>>>>>>>
            try
            {
                Dictionary<string, dynamic> aditional = null;
                if (error_msg["context"] != "")
                {
                    aditional = new Dictionary<string, dynamic>() { { "causal", $"{error_msg["context"]}" } };
                }
                obj_logger.after_app_request_service(output, typeof(notificacionRecaudoBCSResponse), true, aditional, id_trx);
            }
            catch (Exception ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", false},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
            }

            return output;
        }

        [OperationBehavior]
        public notificacionRecaudoBCSResponse reversoNotificacionRecaudoBCS(transactionXMLNotificacionRequest input)
        {
            notificacionRecaudoBCSResponse output = new notificacionRecaudoBCSResponse();
            output.transactionXML = new transactionXMLNotResponse();
            output.transactionXML.parametersXML = new parametersXMLNotResponse();
            output.transactionXML.parametersXML.notificationOfCollectionResponse = new DataContractNotificacionRecaudoBCSResponse();

            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionDate = input.transactionXML.contentXML.notificationOfCollectionRequest.paymentDate;
            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionCode = input.transactionXML.contentXML.notificationOfCollectionRequest.transactionCode;

            Dictionary<string, dynamic> data_flujo = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>();
            string application = "";
            string error_name = "";
            string error_user = "";
            string error_pdp = "";
            LoggerCustom obj_logger = new LoggerCustom(OperationContext.Current);

            //Secuencia log de entrada
            //<<<<<<>>>>>>>>>>
            error_name = "error_log_request";
            error_user = "Error con el log request";
            error_pdp = "Error respuesta PDP: (Error con los logs [Reverso])";
            //<<<<<<>>>>>>>>>>
            try
            {
                obj_logger.before_app_request();
            }
            catch (Exception ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ER";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                return output;
            }

            //llamar al flujo
            Tuple<notificacionRecaudoBCSResponse, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> res_flujo = FlujoReversoNotificacionRecaudoBCS(input, obj_logger);
            output = res_flujo.Item1;
            error_msg = res_flujo.Item2;
            string id_trx = res_flujo.Item3;
            data_flujo = res_flujo.Item4;
            application = (string)data_flujo["application"];
            application = application.ToUpper();
            string terminal = (string)data_flujo["id_terminal"];
            string usuario = (string)data_flujo["id_usuario"];
            string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_REV_REC_EMP_" + application));
            long referencia1 = -1;
            string id_comercio = input.transactionXML.contentXML.notificationOfCollectionRequest.reference1;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(input.transactionXML.contentXML.notificationOfCollectionRequest.reference1);
            }

            DateTime fecha = DateTime.UtcNow.Subtract(TimeSpan.FromHours(5));
            decimal totalValue = 0;
            if (decimal.TryParse(input.transactionXML.contentXML.notificationOfCollectionRequest.totalValue, out totalValue))
            {
                totalValue = totalValue / 100;
            }

            //Secuencia registrar en tbl_recaudo_integrado_banco
            if (output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode != "OK")
            {
                Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>(){
                        { "id_trx", (id_trx != "") ? id_trx : null },
                        { "id_log_trx", obj_logger.id_log },
                        { "nombre_banco", "caja_social" },
                        { "valor_trx",totalValue.ToString().Replace(",",".") },
                        { "fecha_trx",fecha},
                        { "res_obj", new Dictionary<string, dynamic>(){
                                { "status",false},
                                { "msg","Error reverso recaudo "+output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription},
                                { "obj", error_msg }                   
                        }},
                        { "status_trx",false}, 
                        { "data_contingencia", new Dictionary<string, dynamic>(){} },
                        { "id_tipo_transaccion", type_trx_rec },
                        { "name_tipo_transaccion", "Caja social - Reverso recaudo" },
                        { "inf_comercio", new Dictionary<string, dynamic>(){
                                    {"id_comercio",referencia1 },
                                    {"id_usuario", usuario},
                                    {"id_terminal", terminal}
                        }},
                        { "fecha_trx_asincrona", fecha},
                        { "is_trx_contingencia", false}
                    };
                try
                {
                    queries_base query_helper = new queries_base();
                    query_helper.Insertar("tbl_recaudo_integrado_bancos", data_insert, application);
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ER";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    return output;
                }

                //Secuencia actualizar registro id trx 
                //<<<<<<>>>>>>>>>>
                error_name = "error_log_response";
                error_user = "Error con al modificar el response en los logs id trx";
                error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
                //<<<<<<>>>>>>>>>>
                try
                {
                    trx_service RealizarPeticion = new trx_service(obj_logger);
                    RealizarPeticion.RealizarPeticionPut(id_trx, type_trx_rec, "Error reverso recaudo", data_insert, application, false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };

                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ER";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    return output;
                }
            }

            //Secuencia response log
            //<<<<<<>>>>>>>>>>
            error_name = "error_log_response";
            error_user = "Error con response log";
            error_pdp = "Error respuesta PDP: (Error con los logs)";
            //<<<<<<>>>>>>>>>>
            try
            {
                Dictionary<string, dynamic> aditional = null;
                if (error_msg["context"] != "")
                {
                    aditional = new Dictionary<string, dynamic>() { { "causal", $"{error_msg["context"]}" } };
                }
                obj_logger.after_app_request_service(output, typeof(notificacionRecaudoBCSResponse), true, aditional, id_trx);
            }
            catch (Exception ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", false},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
            }

            return output;
        }

    }

}
