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
        {}

        [OperationFlujo]

        public Tuple<responseMsgB2B, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoConsultaRecaudoBCS(requestMsgB2B _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            TransactionXML input = _input.transactionXML;

            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new TransactionXML();

            faultServiceB2BException theFault = new faultServiceB2BException();
            theFault.error = new ErrorTransactionXML();

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
    }
}


        