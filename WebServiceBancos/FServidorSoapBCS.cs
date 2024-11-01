using System;
using System.Globalization;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Web;
using Npgsql;

using static WebServiceBancos.ClassSchemaCustom;
using WebServiceBancos.Logs;
using WebServiceBancos.templates_lib;
using WebServiceBancos.ConexionBD;
using WebServiceBancos.CServidorSoapBCS;
using System.Drawing;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Eventing.Reader;
using System.Web.Services.Protocols;
using System.Web.Services;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Data.SqlTypes;

namespace WebServiceBancos
{
    public class FServidorSoapBCS
    {

        static bool ContainsNonZeroCharacter(string input)
        {
            foreach (char c in input)
            {
                if (c != '0')
                {
                    return true;
                }
            }
            return false;
        }

        public static string SerializeObjectToXmlString(object objeto, Type tipoObjeto)
        {
            DataContractSerializer serializer = new DataContractSerializer(tipoObjeto);

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(ms))
                {
                    serializer.WriteObject(writer, objeto);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        static private bool IsValidSecurity(string username_, string password_)
        {
            string username = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("USERNAME"));
            string password = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("PASSWORD"));

            return username_ == username && password_ == password;
        }

        public class OperationFlujo : Attribute
        { }

        [OperationFlujo]
        public Tuple<responseMsgB2B, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoConsultaRecaudoBCS(requestMsgB2B _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            transactionXML input = _input.transactionXML;
            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new transactionXML();

            XElement content = XElement.Parse(input.contentXML);
            XElement parameters = XElement.Parse(input.parametersXML);

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

            ConstantesBCS constantes_bcs = new ConstantesBCS();
            string error_name = "";
            string error_user = "";
            string error_pdp = "";

            //Secuencia realizar consulta del comercio
            //<<<<<<>>>>>>>>>>
            error_name = "error_bd_consult_comercio";
            error_user = "Error al consultar el comercio en la bases de datos";
            error_pdp = "Error respuesta: (Error al consultar en la bases de datos)";
            //<<<<<<>>>>>>>>>>
            (List<Dictionary<string, dynamic>>, string) comercio = (new List<Dictionary<string, dynamic>>(), "");
            List<Dictionary<string, dynamic>> result = new List<Dictionary<string, dynamic>>();
            string id_usuario = "-1";
            string id_terminal = "-1";
            string nombre_usuario = "SIN NOMBRE";
            string application = "pdp";

            long referencia1 = -1;
            string id_comercio = content.Element("reference1")?.Value;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(id_comercio);
            }

            try
            {
                try
                {
                    queries_base query_helper = new queries_base();
                    comercio = query_helper.ConsultarComercio(Convert.ToInt32(referencia1));
                    result = comercio.Item1;
                    application = comercio.Item2;
                    if (result.Count != 0)
                    {
                        Dictionary<string, dynamic> resultado = result[0];
                        if (resultado["id_usuario_suser"] != null)
                        {
                            int usuario = (int)resultado["id_usuario_suser"];
                            id_usuario = Convert.ToString(usuario);
                        }
                        if (resultado["id_terminal"] != null)
                        {
                            int terminal = (int)resultado["id_terminal"];
                            id_terminal = Convert.ToString(terminal);
                        }
                        if (resultado["nombre_usuario"] != null)
                        {
                            string nombre = (string)resultado["nombre_usuario"];
                            nombre_usuario = nombre;
                        }
                    }
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
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                Out_fallida.totalValue = 0;
                Out_fallida.responseCode = "ER";
                Out_fallida.responseError.errorCode = "00001";
                Out_fallida.responseError.errorType = "GEN";
                Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                // Serializar el objeto a XML
                string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlStringFallida;
            }

            string id_trx = "0";
            application = application.ToUpper();
            res_flujo = new Dictionary<string, dynamic>()
            {
                { "application", application},
                { "id_terminal", id_terminal},
                { "id_usuario", id_usuario},
                { "nombre_usuario", nombre_usuario},
            };

            // Secuencia generar id trx
            //<<<<<<>>>>>>>>>>
            error_user = "Error al llamar al servicio de trx";
            error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
            string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_CONS_REC_EMP_" + application));
            try
            {
                Dictionary<string, dynamic> body = new Dictionary<string, dynamic>()
                    {
                        {"status",false},
                        {"codigo",500},
                        {"msg","Caja social - consulta: Error consulta recaudo" },
                        {"obj",new Dictionary<string,dynamic>(){
                            {"Message",$"{Out_fallida.responseDescription}"},
                            {"obj",(id_comercio != "") ? Convert.ToInt64(referencia1) : (long?)null}
                        } }
                    };
                trx_service RealizarPeticion = new trx_service(OptionalLoggerCustom);
                id_trx = RealizarPeticion.RealizarPeticionPost(body, Out_exitosa.totalValue, type_trx_rec, referencia1, id_terminal, id_usuario, nombre_usuario, application).GetAwaiter().GetResult();
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                Out_fallida.totalValue = 0;
                Out_fallida.responseCode = "ER";
                Out_fallida.responseError.errorCode = "00001";
                Out_fallida.responseError.errorType = "GEN";
                Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                // Serializar el objeto a XML
                string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlStringFallida;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            //Secuencia Verificar los datos de entrada del soap
            //<<<<<<>>>>>>>>>>
            error_name = "error_input_data_service";
            error_user = "Error con los datos de entrada del servicio";
            error_pdp = "Error respuesta: (Error con los datos de entrada del servicio [consultaRecaudoBCS])";
            decimal totalValue = 0;
            //<<<<<<>>>>>>>>>>
            try
            {
                List<string> errores_schema = new List<string>();
                //EANCode
                string value_ean;
                if (constantes_bcs.ean.TryGetValue(content.Element("EANCode")?.Value, out value_ean) is false)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Código EAN inválido");
                    Out_fallida.responseError.errorCode = "00006";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference2
                if (content.Element("reference2")?.Value.Length == 0 || ContainsNonZeroCharacter(content.Element("reference2")?.Value))
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Factura inválida");
                    Out_fallida.responseError.errorCode = "00010";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference3
                if (content.Element("reference3")?.Value.Length == 0 || ContainsNonZeroCharacter(content.Element("reference3")?.Value))
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Factura inválida");
                    Out_fallida.responseError.errorCode = "00010";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //day
                if (content.Element("day")?.Value != "N" && content.Element("day")?.Value != "A")
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Horario no permitido para realizar consultas");
                    Out_fallida.responseError.errorCode = "00002";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento contentXML
                //TransactionCode
                if (content.Element("transactionCode")?.Value.Length == 0 || content.Element("transactionCode")?.Value.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud TransactionCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //EANCode
                if (content.Element("EANCode")?.Value.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud EANCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //officeCode
                if (content.Element("officeCode")?.Value.Length == 0 || content.Element("officeCode")?.Value.Length > 4)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud officeCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference1
                if (content.Element("reference1")?.Value.Length == 0 || content.Element("reference1")?.Value.Length > 24)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.responseDescription = "Error en longitud reference1";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference2
                if (content.Element("reference2")?.Value.Length > 24)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.responseDescription = "Error en longitud reference2";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference3
                if (content.Element("reference3")?.Value.Length > 24)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.responseDescription = "Error en longitud reference3";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //totalValue
                if (content.Element("totalValue")?.Value.ToString().Length > 14)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en longitud totalValue";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (!decimal.TryParse(content.Element("totalValue")?.Value, out totalValue))
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en campo totalValue";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //transactionDate
                if (content.Element("transactionDate")?.Value.Length != 8)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en longitud transactionDate";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //transactionHour
                if (content.Element("transactionHour")?.Value.Length != 6)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en longitud transactionHour";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //expirationDate
                if (content.Element("expirationDate")?.Value.Length != 8)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud expirationDate";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //URL
                if (content.Element("URL")?.Value.Length == 0 || content.Element("URL")?.Value.Length > 500)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud URL";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento parametersXML
                //requestDate
                if (parameters.Element("requestDate")?.Value.Length != 17)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud requestDate";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (parameters.Element("bankCode")?.Value.Length == 0 || parameters.Element("bankCode")?.Value.Length > 4)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud bankCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (parameters.Element("clientCode")?.Value.Length == 0 || parameters.Element("clientCode")?.Value.Length > 16)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud clientCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //service
                if (parameters.Element("service")?.Value.Length == 0 || parameters.Element("service")?.Value.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud service";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementNo
                if (parameters.Element("agreementNo")?.Value.Length == 0 || parameters.Element("agreementNo")?.Value.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud agreementNo";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-key
                if (parameters.Element("parameters")?.Element("key")?.Value.Length == 0 || parameters.Element("parameters")?.Element("key")?.Value.Length > 10)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud parameters key";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-value
                if (parameters.Element("parameters")?.Element("value")?.Value.Length == 0 || parameters.Element("parameters")?.Element("value")?.Value.Length > 30)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud parameters value";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (errores_schema.Count != 0)
                {
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
            }
            catch (SchemaCustomException ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out_fallida.responseCode = "ER";
                Out_fallida.responseError.errorType = "B2B";
                Out_fallida.responseError.errorDescription = $"{error_msg["context"]}";
                Out_fallida.totalValue = 0;
                string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlStringFallida;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
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
                Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                Out_fallida.totalValue = 0;
                Out_fallida.responseCode = "ER";
                Out_fallida.responseError.errorCode = "00001";
                Out_fallida.responseError.errorType = "GEN";
                Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlStringFallida;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            try
            {
                try
                {
                    if (result.Count == 0)
                    {
                        error_msg["context"] = "Comercio no existe";
                        Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                        Out_fallida.totalValue = 0;
                        Out_fallida.responseCode = "ER";
                        Out_fallida.responseError.errorType = "B2B";
                        Out_fallida.responseError.errorCode = "00004";
                        Out_fallida.responseError.errorDescription = "Factura no existe";
                        string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                        output.transactionXML.parametersXML = xmlStringFallida;
                        return Tuple.Create(output, error_msg, id_trx, res_flujo);
                    }
                    else
                    {
                        Out_exitosa.responseCode = "OK";
                        Out_exitosa.responseDescription = "Operación Exitosa";
                        Out_exitosa.totalValue = totalValue;
                        // Serializar el objeto a XML
                        string xmlStringExitosa = SerializeObjectToXmlString(Out_exitosa, typeof(DataContractConsultaRecaudoBCSResponseExitosa));
                        output.transactionXML.parametersXML = xmlStringExitosa;
                        return Tuple.Create(output, error_msg, id_trx, res_flujo);
                    }
                }
                catch (Exception excep)
                {
                    throw new Exception($"Exception = {excep.Message}", excep);
                }
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", excep},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                Out_fallida.totalValue = 0;
                Out_fallida.responseCode = "ER";
                Out_fallida.responseError.errorCode = "00001";
                Out_fallida.responseError.errorType = "GEN";
                Out_fallida.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out_fallida.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlStringFallida = SerializeObjectToXmlString(Out_fallida, typeof(DataContractConsultaRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlStringFallida;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }
        }

        [OperationFlujo]
        public Tuple<responseMsgB2B, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoNotificacionRecaudoBCS(requestMsgB2B _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            transactionXML input = _input.transactionXML;
            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new transactionXML();

            XElement content = XElement.Parse(input.contentXML);
            XElement parameters = XElement.Parse(input.parametersXML);

            DataContractNotificacionRecaudoBCSResponse Out = new DataContractNotificacionRecaudoBCSResponse();
            Out.transactionDate = content.Element("paymentDate")?.Value;
            Out.transactionCode = content.Element("transactionCode")?.Value;

            ConstantesBCS constantes_bcs = new ConstantesBCS();
            string error_name = "";
            string error_user = "";
            string error_pdp = "";

            //Secuencia realizar consulta del comercio
            //<<<<<<>>>>>>>>>>
            error_name = "error_bd_consult_comercio";
            error_user = "Error al consultar el comercio en la bases de datos";
            error_pdp = "Error respuesta: (Error al consultar en la bases de datos)";
            //<<<<<<>>>>>>>>>>
            (List<Dictionary<string, dynamic>>, string) comercio = (new List<Dictionary<string, dynamic>>(), "");
            List<Dictionary<string, dynamic>> result = new List<Dictionary<string, dynamic>>();
            string id_usuario = "-1";
            string id_terminal = "-1";
            string nombre_usuario = "SIN NOMBRE";
            string application = "PDP";
            long referencia1 = -1;
            string id_comercio = content.Element("reference1")?.Value;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(id_comercio);
            }

            try
            {
                try
                {
                    queries_base query_helper = new queries_base();
                    comercio = query_helper.ConsultarComercio(Convert.ToInt32(referencia1));
                    result = comercio.Item1;
                    application = comercio.Item2;
                    if (result.Count != 0)
                    {
                        Dictionary<string, dynamic> resultado = result[0];
                        if (resultado["id_usuario_suser"] != null)
                        {
                            int usuario = (int)resultado["id_usuario_suser"];
                            id_usuario = Convert.ToString(usuario);
                        }
                        if (resultado["id_terminal"] != null)
                        {
                            int terminal = (int)resultado["id_terminal"];
                            id_terminal = Convert.ToString(terminal);
                        }
                        if (resultado["nombre_usuario"] != null)
                        {
                            string nombre = (string)resultado["nombre_usuario"];
                            nombre_usuario = nombre;
                        }
                    }
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
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
            }

            //Secuencia generar id trx 
            //<<<<<<>>>>>>>>>>
            error_user = "Error al llamar al servicio de trx";
            error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
            //<<<<<<>>>>>>>>>>
            string id_trx = "0";
            string aplicacion_sqs = application;
            application = application.ToUpper();
            res_flujo = new Dictionary<string, dynamic>()
                {
                { "application", application},
                { "id_terminal", id_terminal},
                { "id_usuario", id_usuario},
                { "nombre_usuario", nombre_usuario},
            };
            string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_REC_EMP_" + application));
            decimal totalValue = 0;
            if (decimal.TryParse(content.Element("totalValue")?.Value, out totalValue))
            {
                totalValue = totalValue / 100;
            }
            else
            {
                Out.responseDescription = "Error en campo totalValue";
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                // Serializar el objeto a XML
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            try
            {
                Dictionary<string, dynamic> body = new Dictionary<string, dynamic>()
                    {
                        {"status",false},
                        {"codigo",500},
                        {"msg","Caja social - notificación: Error notificación recaudo" },
                        {"obj",new Dictionary<string,dynamic>(){
                            {"Message",$"{Out.responseError.errorDescription}"},
                            {"referencia1",(id_comercio != "") ? Convert.ToInt64(referencia1) : (long?)null}
                        } }
                    };
                trx_service RealizarPeticion = new trx_service(OptionalLoggerCustom);
                id_trx = RealizarPeticion.RealizarPeticionPost(body, totalValue, type_trx_rec, referencia1, id_terminal, id_usuario, nombre_usuario, application).GetAwaiter().GetResult();
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            //Secuencia Verificar los datos de entrada del soap
            //<<<<<<>>>>>>>>>>
            error_name = "error_input_data_service";
            error_user = "Error con los datos de entrada del servicio";
            error_pdp = "Error respuesta: (Error con los datos de entrada del servicio [notificarRecaudoBCS])";
            //<<<<<<>>>>>>>>>>
            try
            {
                List<string> errores_schema = new List<string>();
                //EANCode
                string value_ean;
                if (constantes_bcs.ean.TryGetValue(content.Element("EANCode")?.Value, out value_ean) is false)
                {
                    errores_schema.Add("Código EAN inválido");
                    Out.responseError.errorCode = "00006";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference2
                if (content.Element("reference2")?.Value.Length == 0 || ContainsNonZeroCharacter(content.Element("reference2")?.Value))
                {
                    errores_schema.Add("Factura inválida");
                    Out.responseError.errorCode = "00010";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //PaymentForm
                string value_payment_form;
                if (constantes_bcs.formaPago.TryGetValue(content.Element("paymentForm")?.Value, out value_payment_form) is false)
                {
                    errores_schema.Add("Forma de pago no válida");
                    Out.responseError.errorCode = "00017";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue y totalvalue
                if (content.Element("cashValue")?.Value == "0" || content.Element("totalValue")?.Value == "0")
                {
                    errores_schema.Add("Valor de transacción no permitido");
                    Out.responseError.errorCode = "00018";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento contentXML
                //TransactionCode
                if (content.Element("transactionCode")?.Value.Length != 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud TransactionCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementCode
                if (content.Element("agreementCode")?.Value.Length == 0 || content.Element("agreementCode")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud agreementCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementBankCode
                if (content.Element("agreementBankCode")?.Value.Length == 0 || content.Element("agreementBankCode")?.Value.Length > 2)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud agreementBankCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (content.Element("clientCode")?.Value.Length == 0 || content.Element("clientCode")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud clientCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //EANCode
                if (content.Element("EANCode")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud EANCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //accountNumber
                if (content.Element("accountNumber")?.Value.Length == 0 || content.Element("accountNumber")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud accountNumber";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference1
                if (content.Element("reference1")?.Value.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud reference1";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference2
                if (content.Element("reference2")?.Value.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud reference2";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue
                decimal cashValue;
                if (!decimal.TryParse(content.Element("cashValue")?.Value, out cashValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en campo cashValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (content.Element("cashValue")?.Value.Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud cashValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //checkValue
                decimal checkValue;
                if (!decimal.TryParse(content.Element("checkValue")?.Value, out checkValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en campo checkValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (content.Element("checkValue")?.Value.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud checkValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //totalValue
                if (content.Element("totalValue")?.Value.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud totalValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentForm
                if (content.Element("paymentForm")?.Value.Length > 1)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud paymentForm";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentDate
                if (content.Element("paymentDate")?.Value.Length == 0 || content.Element("paymentDate")?.Value.Length > 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud paymentDate";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //officeCode
                if (content.Element("officeCode")?.Value.Length == 0 || content.Element("officeCode")?.Value.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud officeCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (content.Element("bankCode")?.Value.Length == 0 || content.Element("bankCode")?.Value.Length > 3)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud bankCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //isBatch
                if (content.Element("isBatch")?.Value != "true" && content.Element("isBatch")?.Value != "false")
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en el campo isBatch";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //dealCode
                if (content.Element("dealCode")?.Value.Length == 0 || content.Element("dealCode")?.Value.Length > 50)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud dealCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento parametersXML
                //requestDate
                if (parameters.Element("requestDate")?.Value.Length != 17)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud requestDate";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (parameters.Element("bankCode")?.Value.Length == 0 || parameters.Element("bankCode")?.Value.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud bankCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (parameters.Element("clientCode")?.Value.Length == 0 || parameters.Element("clientCode")?.Value.Length > 16)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud clientCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //service
                if (parameters.Element("service")?.Value.Length == 0 || parameters.Element("service")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud service";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementNo
                if (parameters.Element("agreementNo")?.Value.Length == 0 || parameters.Element("agreementNo")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud agreementNo";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-key
                if (parameters.Element("parameters")?.Element("key")?.Value.Length == 0 || parameters.Element("parameters")?.Element("key")?.Value.Length > 10)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud parameters key";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-value
                if (parameters.Element("parameters")?.Element("value")?.Value.Length == 0 || parameters.Element("parameters")?.Element("value")?.Value.Length > 30)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud parameters value";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (errores_schema.Count != 0)
                {
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
            }
            catch (SchemaCustomException ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorDescription = $"{ex.Message}";
                Out.responseError.errorType = "B2B";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
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
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            try
            {
                if (result.Count == 0)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"Comercio no existe"},
                        { "description", error_user },
                        { "error_pdp", "Factura no existe" },
                    };
                    Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out.responseError.errorType = "B2B";
                    Out.responseCode = "00004";
                    string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                    output.transactionXML.parametersXML = xmlString;
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };

                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            //Secuencia realizar consulta del recaudo 
            //<<<<<<>>>>>>>>>>
            error_name = "error_bd_consult_recaudo";
            error_user = "Error al consultar el recaudo en la bases de datos";
            error_pdp = "Error respuesta: (Error al consultar en la bases de datos)";
            //<<<<<<>>>>>>>>>>
            DateTime fecha = DateTime.UtcNow.Subtract(TimeSpan.FromHours(5));

            try
            {
                try
                {
                    Dictionary<string, dynamic> data_search = new Dictionary<string, dynamic>()
                    {
                        {"codigo confirmacion recaudo", content.Element("dealCode")?.Value},
                        {"valor_trx", totalValue.ToString().Replace(",",".")},
                        {"referencia 1", referencia1.ToString()},
                    };
                    queries_base query_helper = new queries_base();
                    List<Dictionary<string, dynamic>> result_duplicado = query_helper.ConsultarRecaudo("tbl_recaudo_integrado_bancos", data_search, application);
                    Console.WriteLine(result_duplicado);
                    if (result_duplicado.Count != 0)
                    {
                        Out.responseCode = "ERDUPLICATED";
                        Out.responseDescription = "Duplicado en datos de recaudo";
                        Out.responseError.errorType = "B2B";
                        string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                        output.transactionXML.parametersXML = xmlString;
                        return Tuple.Create(output, error_msg, id_trx, res_flujo);
                    }
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
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            //Secuencia enviar mensaje a la sqs de recaudo integrado pdp
            //<<<<<<>>>>>>>>>>
            error_name = "error_sqs";
            error_user = "Error con el mensaje enviado a la sqs de recaudo integrado pdp";
            error_pdp = "Error respuest: (Error con el envio del mensaje a la sqs)";
            Console.WriteLine(aplicacion_sqs);
            //<<<<<<>>>>>>>>>>
            try
            {
                try
                {
                    Dictionary<string, dynamic> data_contingencia = new Dictionary<string, dynamic>()
                    {
                        {"codigo confirmacion recaudo",content.Element("dealCode")?.Value},
                        {"referencia 1",referencia1},
                    };

                    DateTime fecha_trx_asincrona = OptionalLoggerCustom.fecha_start;
                    var converted_fecha_trx_asincrona = Convert.ToDateTime(fecha_trx_asincrona);

                    handle_sqs_utils recaudo_empresarial_pdp_sqs = new handle_sqs_utils(OptionalLoggerCustom);
                    recaudo_empresarial_pdp_sqs.handle_recaudo_empresarial_pdp_sqs(
                        proceso: 2,
                        id_comercio: referencia1,
                        banco: "caja_social",
                        valor_total_trx: totalValue,
                        data_contingencia: data_contingencia,
                        is_trx_contingencia: false,
                        fecha_trx_asincrona: converted_fecha_trx_asincrona.ToString("yyyy-MM-dd HH:mm:ss.ff"),
                        application: aplicacion_sqs,
                        id_log: OptionalLoggerCustom.id_log,
                        id_trx: id_trx
                    );
                }
                catch (SqlException ex)
                {
                    throw new Exception($"SqlException = {ex.Message}", ex);
                }
                catch (Aws.Sqs.RequestSqsLoggerCustomException ex)
                {
                    error_name = "error_logger_request_sqs";
                    error_user = $"{ex.Message_user}";
                    error_pdp = "Error respuesta: (Error regitro de logger sqs)";
                    throw new Exception($"{ex.Message}", ex);
                }
                catch (Aws.Sqs.ResponseSqsLoggerCustomException ex)
                {
                    error_name = "error_logger_response_sqs";
                    error_user = $"{ex.Message_user}";
                    error_pdp = "Error respuesta: (Error regitro de logger sqs)";
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception = {ex.Message}", ex);
                }
            }
            catch (Aws.Sqs.ResponseSqsLoggerCustomException ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", false},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                throw new Exception($"Aws.Sqs.ResponseSqsLoggerCustomException = {ex.Message}", ex);
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
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            Out.responseCode = "OK";
            string xmlString_ = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
            output.transactionXML.parametersXML = xmlString_;
            return Tuple.Create(output, error_msg, id_trx, res_flujo);
        }

        [OperationFlujo]
        public Tuple<responseMsgB2B, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoReversoNotificacionRecaudoBCS(requestMsgB2B _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            transactionXML input = _input.transactionXML;
            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new transactionXML();

            XElement content = XElement.Parse(input.contentXML);
            XElement parameters = XElement.Parse(input.parametersXML);

            DataContractNotificacionRecaudoBCSResponse Out = new DataContractNotificacionRecaudoBCSResponse();
            Out.transactionDate= content.Element("paymentDate")?.Value;
            Out.transactionCode = content.Element("transactionCode")?.Value;

            ConstantesBCS constantes_bcs = new ConstantesBCS();
            string error_name = "";
            string error_user = "";
            string error_pdp = "";

            //Secuencia realizar consulta del comercio
            //<<<<<<>>>>>>>>>>
            error_name = "error_bd_consult_comercio";
            error_user = "Error al consultar el comercio en la bases de datos";
            error_pdp = "Error respuesta: (Error al consultar en la bases de datos)";
            //<<<<<<>>>>>>>>>>
            (List<Dictionary<string, dynamic>>, string) comercio = (new List<Dictionary<string, dynamic>>(), "");
            List<Dictionary<string, dynamic>> result = new List<Dictionary<string, dynamic>>();
            string id_usuario = "-1";
            string id_terminal = "-1";
            string nombre_usuario = "SIN NOMBRE";
            string application = "PDP";
            long referencia1 = -1;
            string id_comercio = content.Element("reference1")?.Value;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(id_comercio);
            }

            try
            {
                try
                {
                    queries_base query_helper = new queries_base();
                    comercio = query_helper.ConsultarComercio(Convert.ToInt32(referencia1));
                    result = comercio.Item1;
                    application = comercio.Item2;
                    if (result.Count != 0)
                    {
                        Dictionary<string, dynamic> resultado = result[0];
                        if (resultado["id_usuario_suser"] != null)
                        {
                            int usuario = (int)resultado["id_usuario_suser"];
                            id_usuario = Convert.ToString(usuario);
                        }
                        if (resultado["id_terminal"] != null)
                        {
                            int terminal = (int)resultado["id_terminal"];
                            id_terminal = Convert.ToString(terminal);
                        }
                        if (resultado["nombre_usuario"] != null)
                        {
                            string nombre = (string)resultado["nombre_usuario"];
                            nombre_usuario = nombre;
                        }
                    }
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
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
            }

            //Secuencia realizar consulta del recaudo 
            //<<<<<<>>>>>>>>>>
            error_name = "error_bd_consult_recaudo";
            error_user = "Error al consultar el recaudo en la bases de datos";
            error_pdp = "Error respuesta: (Error al consultar en la bases de datos)";
            //<<<<<<>>>>>>>>>>
            DateTime fecha = DateTime.UtcNow.Subtract(TimeSpan.FromHours(5));
            bool reversar = true; //Se asume que debería reversarse
            bool status = true; //Se asume que previamente fue aprobada
            string id_trx = "0";
            decimal totalValue = 0;
            string aplicacion_sqs = application;
            application = application.ToUpper();
            res_flujo = new Dictionary<string, dynamic>()
                {
                { "application", application},
                { "id_terminal", id_terminal},
                { "id_usuario", id_usuario},
                { "nombre_usuario", nombre_usuario},
            };

            if (decimal.TryParse(content.Element("totalValue")?.Value, out totalValue))
            {
                totalValue = totalValue / 100;
            }
            else
            {
                Out.responseDescription = "Error en campo totalValue";
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            try
            {
                try
                {
                    Dictionary<string, dynamic> data_search = new Dictionary<string, dynamic>()
                    {
                        {"codigo confirmacion recaudo",content.Element("dealCode")?.Value},
                        {"valor_trx",totalValue.ToString().Replace(",",".")},
                        {"referencia 1",referencia1.ToString()},
                    };
                    queries_base query_helper = new queries_base();
                    List<Dictionary<string, dynamic>> result_duplicado = query_helper.ConsultarRecaudo("tbl_recaudo_integrado_bancos", data_search, application);
                    Console.WriteLine(result_duplicado);
                    if (result_duplicado.Count != 0)
                    {
                        Dictionary<string, dynamic> resultado = result_duplicado[0];
                        long id = (long)resultado["id_trx"];
                        id_trx = (string)id.ToString();
                        status = (bool)resultado["status_trx"];
                    }
                    else
                    {
                        Out.responseError.errorCode = "00015";
                        Out.responseError.errorType = "B2B";
                        Out.responseError.errorDescription = "No existe transacción a reversar";
                        reversar = false;  //No debería reversarse y debe iniciar trx
                        status = false;
                    }
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
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
            }

            //Secuencia generar id trx solo sí no existe una trx con las llaves dealcode, totalvalue y reference//
            //<<<<<<>>>>>>>>>>
            error_name = "error_bd_trx";
            error_user = "Error al llamar al servicio de trx ";
            error_pdp = "Error respuesta: Fallo al consumir servicio de transacciones [0010009]";
            //<<<<<<>>>>>>>>>>
            string type_trx_rec = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("TYPE_TRX_REV_REC_EMP_" + application));
            Dictionary<string, dynamic> body = new Dictionary<string, dynamic>()
                {
                    {"status",false},
                    {"codigo",500},
                    {"msg","Caja social - reverso: Error reverso notificación recaudo" },
                    {"obj",new Dictionary<string,dynamic>(){
                        {"Message",$"{Out.responseError.errorDescription}"},
                        {"referencia1",(content.Element("reference1")?.Value != "") ? Convert.ToInt64(referencia1) : (long?)null}
                    } }
                };
            trx_service RealizarPeticion = new trx_service(OptionalLoggerCustom);

            if (!reversar) ///////// Hacer un registro en transacciones que retorne id_trx nuevo
            {
                try
                {
                    id_trx = RealizarPeticion.RealizarPeticionPost(body, totalValue, type_trx_rec, referencia1, id_terminal, id_usuario, nombre_usuario, application).GetAwaiter().GetResult();
                }
                catch (Exception excep)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"Exception = {excep.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    Out.responseError.errorCode = "00001";
                    Out.responseError.errorType = "GEN";
                    Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                    output.transactionXML.parametersXML = xmlString;
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
            }
            else ///////// Hacer un registro en transacciones con el mismo id_trx
            {
                try
                {
                    List<dynamic> trx_reverso = RealizarPeticion.RealizarPeticionGet(id_trx, type_trx_rec, application).GetAwaiter().GetResult();
                    if (trx_reverso.Count != 0)
                    {
                        Dictionary<string, dynamic> obj = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(trx_reverso[0]);
                        Console.Write(obj);
                        bool status_trx_reverso = (bool)JsonSerializer.Deserialize<bool>(obj["status_trx"]);
                        if (status_trx_reverso)
                        {
                            id_trx = RealizarPeticion.RealizarPeticionPost(body, totalValue, type_trx_rec, referencia1, id_terminal, id_usuario, nombre_usuario, application).GetAwaiter().GetResult();
                            Out.responseCode = "ERDUPLICATED";
                            Out.responseDescription = "Duplicado en datos de recaudo";
                            Out.responseError.errorType = "B2B";
                            string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                            output.transactionXML.parametersXML = xmlString;
                            return Tuple.Create(output, error_msg, id_trx, res_flujo);
                        }
                        else
                        {
                            RealizarPeticion.RealizarPeticionPut(id_trx, type_trx_rec, "Error reverso recaudo", body, application, false).GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        id_trx = RealizarPeticion.RealizarPeticionPost(body, totalValue, type_trx_rec, referencia1, id_terminal, id_usuario, nombre_usuario, application, long.Parse(id_trx)).GetAwaiter().GetResult();
                    }
                }
                catch (Exception excep)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"Exception = {excep.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    Out.responseError.errorCode = "00001";
                    Out.responseError.errorType = "GEN";
                    Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                    output.transactionXML.parametersXML = xmlString;
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
            }

            //Secuencia Verificar los datos de entrada del soap
            //<<<<<<>>>>>>>>>>
            error_name = "error_input_data_service";
            error_user = "Error con los datos de entrada del servicio";
            error_pdp = "Error respuesta: (Error con los datos de entrada del servicio [notificarRecaudoBCS])";
            //<<<<<<>>>>>>>>>>
            try
            {
                List<string> errores_schema = new List<string>();
                //EANCode
                string value_ean;
                if (constantes_bcs.ean.TryGetValue(content.Element("EANCode")?.Value, out value_ean) is false)
                {
                    errores_schema.Add("Código EAN inválido");
                    Out.responseError.errorCode = "00006";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference2
                if (content.Element("reference2")?.Value.Length == 0 || ContainsNonZeroCharacter(content.Element("reference2")?.Value))
                {
                    errores_schema.Add("Factura inválida");
                    Out.responseError.errorCode = "00010";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //PaymentForm
                string value_payment_form;
                if (constantes_bcs.formaPago.TryGetValue(content.Element("paymentForm")?.Value, out value_payment_form) is false)
                {
                    errores_schema.Add("Forma de pago no válida");
                    Out.responseError.errorCode = "00017";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue y totalvalue
                if (content.Element("cashValue")?.Value == "0" || content.Element("totalValue")?.Value == "0")
                {
                    errores_schema.Add("Valor de transacción no permitido");
                    Out.responseError.errorCode = "00018";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento contentXML
                //TransactionCode
                if (content.Element("transactionCode")?.Value.Length != 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud TransactionCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementCode
                if (content.Element("agreementCode")?.Value.Length == 0 || content.Element("agreementCode")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud agreementCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementBankCode
                if (content.Element("agreementBankCode")?.Value.Length == 0 || content.Element("agreementBankCode")?.Value.Length > 2)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud agreementBankCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (content.Element("clientCode")?.Value.Length == 0 || content.Element("clientCode")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud clientCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //EANCode
                if (content.Element("EANCode")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud EANCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //accountNumber
                if (content.Element("accountNumber")?.Value.Length == 0 || content.Element("accountNumber")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud accountNumber";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference1
                if (content.Element("reference1")?.Value.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud reference1";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference2
                if (content.Element("reference2")?.Value.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud reference2";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue
                decimal cashValue;
                if (!decimal.TryParse(content.Element("cashValue")?.Value, out cashValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en campo cashValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (content.Element("cashValue")?.Value.Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud cashValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //checkValue
                decimal checkValue;
                if (!decimal.TryParse(content.Element("checkValue")?.Value, out checkValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en campo checkValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (content.Element("checkValue")?.Value.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud checkValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //totalValue
                if (content.Element("totalValue")?.Value.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud totalValue";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentForm
                if (content.Element("paymentForm")?.Value.Length > 1)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud paymentForm";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentDate
                if (content.Element("paymentDate")?.Value.Length == 0 || content.Element("paymentDate")?.Value.Length > 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud paymentDate";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //officeCode
                if (content.Element("officeCode")?.Value.Length == 0 || content.Element("officeCode")?.Value.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud officeCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (content.Element("bankCode")?.Value.Length == 0 || content.Element("bankCode")?.Value.Length > 3)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud bankCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //isBatch
                if (content.Element("isBatch")?.Value != "true" && content.Element("isBatch")?.Value != "false")
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en el campo isBatch";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //dealCode
                if (content.Element("dealCode")?.Value.Length == 0 || content.Element("dealCode")?.Value.Length > 50)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud dealCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento parametersXML
                //requestDate
                if (parameters.Element("requestDate")?.Value.Length != 17)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud requestDate";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (parameters.Element("bankCode")?.Value.Length == 0 || parameters.Element("bankCode")?.Value.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud bankCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (parameters.Element("clientCode")?.Value.Length == 0 || parameters.Element("clientCode")?.Value.Length > 16)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud clientCode";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //service
                if (parameters.Element("service")?.Value.Length == 0 || parameters.Element("service")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud service";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementNo
                if (parameters.Element("agreementNo")?.Value.Length == 0 || parameters.Element("agreementNo")?.Value.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud agreementNo";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-key
                if (parameters.Element("parameters")?.Element("key")?.Value.Length == 0 || parameters.Element("parameters")?.Element("key")?.Value.Length > 10)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud parameters key";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-value
                if (parameters.Element("parameters")?.Element("value")?.Value.Length == 0 || parameters.Element("parameters")?.Element("value")?.Value.Length > 30)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    Out.responseDescription = "Error en longitud parameters value";
                    Out.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (errores_schema.Count != 0)
                {
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
            }
            catch (SchemaCustomException ex)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"{ex.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };
                Out.responseError.errorDescription = $"{ex.Message}";
                Out.responseError.errorType = "B2B";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
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
                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            try
            {
                if (result.Count == 0)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", true},
                        { "context", $"Comercio no existe"},
                        { "description", error_user },
                        { "error_pdp", "Factura no existe" },
                    };
                    Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out.responseError.errorType = "B2B";
                    Out.responseCode = "00004";
                    string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                    output.transactionXML.parametersXML = xmlString;
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
            }
            catch (Exception excep)
            {
                error_msg = new Dictionary<string, dynamic>()
                {
                    { "name", error_name},
                    { "blocking", true},
                    { "context", $"Exception = {excep.Message}"},
                    { "description", error_user },
                    { "error_pdp", error_pdp },
                };

                Out.responseError.errorCode = "00001";
                Out.responseError.errorType = "GEN";
                Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                output.transactionXML.parametersXML = xmlString;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }
            ///////////////
            if (status)
            {
                //Secuencia enviar mensaje a la sqs de recaudo integrado pdp
                //<<<<<<>>>>>>>>>>
                error_name = "error_sqs";
                error_user = "Error con el mensaje enviado a la sqs de recaudo integrado pdp";
                error_pdp = "Error respuesta: (Error con el envío del mensaje a la sqs)";
                //<<<<<<>>>>>>>>>>
                try
                {
                    try
                    {
                        Dictionary<string, dynamic> data_contingencia = new Dictionary<string, dynamic>()
                        {
                            {"codigo confirmacion recaudo",content.Element("dealCode")?.Value},
                            {"referencia 1",referencia1},
                        };
                        DateTime fecha_trx_asincrona = OptionalLoggerCustom.fecha_start;
                        var converted_fecha_trx_asincrona = Convert.ToDateTime(fecha_trx_asincrona);

                        handle_sqs_utils recaudo_empresarial_pdp_sqs = new handle_sqs_utils(OptionalLoggerCustom);
                        recaudo_empresarial_pdp_sqs.handle_recaudo_empresarial_pdp_sqs(
                            proceso: 3,
                            id_comercio: referencia1,
                            banco: "caja_social",
                            valor_total_trx: totalValue,
                            data_contingencia: data_contingencia,
                            is_trx_contingencia: false,
                            fecha_trx_asincrona: converted_fecha_trx_asincrona.ToString("yyyy-MM-dd HH:mm:ss.ff"),
                            application: aplicacion_sqs,
                            id_log: OptionalLoggerCustom.id_log,
                            id_trx: id_trx
                        );
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"SqlException = {ex.Message}", ex);
                    }
                    catch (Aws.Sqs.RequestSqsLoggerCustomException ex)
                    {
                        error_name = "error_logger_request_sqs";
                        error_user = $"{ex.Message_user}";
                        error_pdp = "Error respuesta PDP: (Error regitro de logger sqs)";
                        throw new Exception($"{ex.Message}", ex);
                    }
                    catch (Aws.Sqs.ResponseSqsLoggerCustomException ex)
                    {
                        error_name = "error_logger_response_sqs";
                        error_user = $"{ex.Message_user}";
                        error_pdp = "Error respuesta PDP: (Error regitro de logger sqs)";
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Exception = {ex.Message}", ex);
                    }
                }
                catch (Aws.Sqs.ResponseSqsLoggerCustomException ex)
                {
                    error_msg = new Dictionary<string, dynamic>()
                    {
                        { "name", error_name},
                        { "blocking", false},
                        { "context", $"{ex.Message}"},
                        { "description", error_user },
                        { "error_pdp", error_pdp },
                    };
                    throw new Exception($"Aws.Sqs.ResponseSqsLoggerCustomException = {ex.Message}", ex);
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
                    Out.responseError.errorCode = "00001";
                    Out.responseError.errorType = "GEN";
                    Out.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    Out.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    string xmlString = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
                    output.transactionXML.parametersXML = xmlString;
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
                Out.responseCode = "OK";
            }
            else
            {
                Out.responseError.errorCode = "00015";
                Out.responseError.errorType = "B2B";
                Out.responseError.errorDescription = "No existe transacción a reversar";
            }
            string xmlString_ = SerializeObjectToXmlString(Out, typeof(DataContractNotificacionRecaudoBCSResponse));
            output.transactionXML.parametersXML = xmlString_;
            return Tuple.Create(output, error_msg, id_trx, res_flujo);
        }
    }
}
        