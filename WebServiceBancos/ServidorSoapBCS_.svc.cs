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
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Drawing.Imaging;

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
        public responseMsgB2B invokeSync(requestMsgB2B input)
        {
            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new TransactionXML();

            faultServiceB2BException theFault = new faultServiceB2BException();
            theFault.error = new ErrorTransactionXML();

            try
            {
                XElement content = XElement.Parse(input.transactionXML.contentXML);
                XElement parameters = XElement.Parse(input.transactionXML.parametersXML);
                Console.WriteLine(content);

                if (content.Name.LocalName == "consultOfCollectionRequest")
                {
                    DataContractConsultaRecaudoBCSResponseExitosa Out_exitosa = new DataContractConsultaRecaudoBCSResponseExitosa();
                    Out_exitosa.paymentReference = content.Element("reference1")?.Value;
                    Out_exitosa.EANCode = content.Element("EANCode")?.Value;
                    Out_exitosa.reference1 = content.Element("reference1")?.Value;
                    Out_exitosa.reference2 = "";
                    Out_exitosa.expirationDate = content.Element("expirationDate")?.Value;
                    Out_exitosa.paymentDate = content.Element("transactionDate")?.Value;
                    Out_exitosa.paymentType = "0";

                    DataContractConsultaRecaudoBCSResponse Out_fallida = new DataContractConsultaRecaudoBCSResponse();
                    Out_fallida.paymentReference = content.Element("reference1")?.Value;
                    Out_fallida.EANCode = content.Element("EANCode")?.Value;
                    Out_fallida.reference1 = content.Element("reference1")?.Value;
                    Out_fallida.reference2 = "";
                    Out_fallida.expirationDate = content.Element("expirationDate")?.Value;
                    Out_fallida.paymentDate = content.Element("transactionDate")?.Value;
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
                        // Serializar el objeto a XML
                        string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                        output.transactionXML.parametersXML = xmlStringFallida;
                        return output;
                    }

                    //llamar al flujo
                    Tuple<responseMsgB2B, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> res_flujo = FlujoConsultaRecaudoBCS(input, obj_logger);
                    output = res_flujo.Item1;
                    error_msg = res_flujo.Item2;
                    string id_trx = res_flujo.Item3;
                    data_flujo = res_flujo.Item4;
                    application = (string)data_flujo["application"];
                    application = application.ToUpper();
                    string terminal = (string)data_flujo["id_terminal"];
                    string usuario = (string)data_flujo["id_usuario"];
                    string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_CONS_REC_EMP_" + application));
                    long referencia1;
                    string id_comercio = content.Element("reference1")?.Value;
                    if (!Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
                    {
                        referencia1 = -1;
                    }
                    else
                    {
                        referencia1 = Convert.ToInt64(id_comercio);
                    }
                    decimal totalValue = 0;
                    if (decimal.TryParse(content.Element("totalValue")?.Value, out totalValue))
                    {
                        totalValue = (totalValue / 100);
                    }

                    Dictionary<string, dynamic> data_insert = new Dictionary<string, dynamic>();
                    string xmlString = Regex.Replace(output.transactionXML.parametersXML, @"[^\x20-\x7E]", string.Empty);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlString.Trim());                    

                    XmlNodeList responseCodeNodes = xmlDoc.GetElementsByTagName("responseCode");
                    XmlNode responseCodeNode = responseCodeNodes[0];
                    string responseCode = responseCodeNode?.InnerText;

                    XmlNodeList responseDescriptionNodes = xmlDoc.GetElementsByTagName("responseDescription");
                    XmlNode responseDescriptionNode = responseDescriptionNodes[0];
                    string responseDescription = responseDescriptionNode?.InnerText;

                    XmlNodeList responseErrorNodes = xmlDoc.GetElementsByTagName("responseError");
                    if (responseErrorNodes.Count > 0)
                    {
                        XmlNode responseErrorNode = responseErrorNodes[0];
                        //string errorCode = responseErrorNode["errorCode"]?.InnerText;
                        string errorDescription = responseErrorNode["errorDescription"]?.InnerText;
                        data_insert = new Dictionary<string, dynamic>(){
                            { "id_trx",(id_trx != "")?id_trx:null},
                            { "id_log_trx", obj_logger.id_log },
                            { "nombre_banco", "caja_social" },
                            { "valor_trx", 0 },
                            { "fecha_trx", crearFechaUtc_m5() },
                            { "res_obj", new Dictionary<string, dynamic>(){
                                { "msg", "Caja social- consulta "+errorDescription},
                                { "status", responseCode.Equals("OK") },
                                { "codigo", responseCode },
                                { "obj", error_msg },
                            } },
                            { "status_trx", responseCode.Equals("OK") },
                            { "data_contingencia", null },
                            { "id_tipo_transaccion", type_trx_rec },
                            { "name_tipo_transaccion", "Caja Social - Consulta Recaudo" },
                            { "inf_comercio", new Dictionary<string, dynamic>(){
                                        {"id_comercio", (id_comercio != "") ? referencia1 : (long?)null},
                                        {"id_usuario", usuario},
                                        {"id_terminal", terminal}
                            }},
                            { "fecha_trx_asincrona", crearFechaUtc_m5()},
                            { "is_trx_contingencia", false}
                        };
                    }
                    else 
                    {
                        data_insert = new Dictionary<string, dynamic>(){
                            { "id_trx",(id_trx != "")?id_trx:null},
                            { "id_log_trx", obj_logger.id_log },
                            { "nombre_banco", "caja_social" },
                            { "valor_trx", totalValue.ToString().Replace(",", ".") },
                            { "fecha_trx", crearFechaUtc_m5() },
                            { "res_obj", new Dictionary<string, dynamic>(){
                                { "msg", "Caja social- consulta "+responseDescription},
                                { "status", responseCode.Equals("OK") },
                                { "codigo", responseCode },
                                { "obj", error_msg },
                            } },
                            { "status_trx", responseCode.Equals("OK") },
                            { "data_contingencia", null },
                            { "id_tipo_transaccion", type_trx_rec},
                            { "name_tipo_transaccion", "Caja Social - Consulta Recaudo" },
                            { "inf_comercio", new Dictionary<string, dynamic>(){
                                        {"id_comercio", (id_comercio != "") ? referencia1 : (long?)null},
                                        {"id_usuario", usuario},
                                        {"id_terminal", terminal}
                            }},
                            { "fecha_trx_asincrona", crearFechaUtc_m5()},
                            { "is_trx_contingencia", false}
                        };
                    }
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
                        RealizarPeticion.RealizarPeticionPut(data_insert["id_trx"], type_trx_rec, data_insert["res_obj"]["msg"], data_insert, application, responseCode.Equals("OK")).GetAwaiter().GetResult();
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
                        // Serializar el objeto a XML
                        string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                        output.transactionXML.parametersXML = xmlStringFallida;
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
                        obj_logger.after_app_request_service(output, typeof(requestMsgB2B), true, aditional, data_insert["id_trx"]);
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

                else if (content.Name.LocalName == "notificationOfCollectionRequest")
                {
                    return output;
                }
                else 
                {
                    theFault.error.errorCode = 1;
                    theFault.error.errorType = "GEN";
                    theFault.error.errorMessage = "CollectionRequest no esperado.";
                    theFault.error.errorDetail = "contentXML: consultOfCollectionRequest ó notificationOfCollectionRequest";
                    throw new FaultException<faultServiceB2BException>(theFault, new FaultReason(theFault.error.errorMessage));
                }                
            }
            catch (Exception exp)
            {
                theFault.error.errorCode = 1;
                theFault.error.errorType = "GEN";
                theFault.error.errorMessage = "Error inesperado";
                theFault.error.errorDetail = exp.Message;
                throw new FaultException<faultServiceB2BException>(theFault, new FaultReason(theFault.error.errorMessage));
            }
        }
    }

}
