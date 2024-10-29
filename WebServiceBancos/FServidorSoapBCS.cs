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

        static private bool IsValidSecurity(string username_, string password_)
        {
            string username = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("USERNAME"));
            string password = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("PASSWORD"));

            return username_ == username && password_ == password;
        }

        public class OperationFlujo : Attribute
        {

        }

        [OperationFlujo]
        public Tuple<consultaRecaudoBCSResponse, Dictionary<string, dynamic>,string, Dictionary<string, dynamic>> FlujoConsultaRecaudoBCS(transactionXMLConsultaRequest _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            transactionXMLConsulta input = _input.transactionXML;

            consultaRecaudoBCSResponse output = new consultaRecaudoBCSResponse();
            output.transactionXML = new transactionXMLResponse();
            output.transactionXML.parametersXML = new parametersXMLResponse();

            DataContractConsultaRecaudoBCSResponseExitosa Out_exitosa = new DataContractConsultaRecaudoBCSResponseExitosa();
            Out_exitosa.paymentReference = input.contentXML.consultOfCollectionRequest.reference1;
            Out_exitosa.EANCode = input.contentXML.consultOfCollectionRequest.EANCode;
            Out_exitosa.reference1 = input.contentXML.consultOfCollectionRequest.reference1;
            Out_exitosa.reference2 = "";
            Out_exitosa.expirationDate = input.contentXML.consultOfCollectionRequest.expirationDate;
            Out_exitosa.paymentDate = input.contentXML.consultOfCollectionRequest.transactionDate;
            Out_exitosa.paymentType = "0";       

            DataContractConsultaRecaudoBCSResponse Out_fallida = new DataContractConsultaRecaudoBCSResponse();
            Out_fallida.paymentReference = input.contentXML.consultOfCollectionRequest.reference1;
            Out_fallida.EANCode = input.contentXML.consultOfCollectionRequest.EANCode;
            Out_fallida.reference1 = input.contentXML.consultOfCollectionRequest.reference1;
            Out_fallida.reference2 = "";
            Out_fallida.expirationDate = input.contentXML.consultOfCollectionRequest.expirationDate;
            Out_fallida.paymentDate = input.contentXML.consultOfCollectionRequest.transactionDate;
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
            (List<Dictionary<string, dynamic>>,string) comercio = (new List<Dictionary<string, dynamic>>(),"");
            List<Dictionary<string, dynamic>> result = new List<Dictionary<string, dynamic>>();
            string id_usuario = "-1";
            string id_terminal = "-1";
            string nombre_usuario = "SIN NOMBRE";
            string application = "pdp";

            long referencia1 = -1;
            string id_comercio = input.contentXML.consultOfCollectionRequest.reference1;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(input.contentXML.consultOfCollectionRequest.reference1);
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
                output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
            }

            //Secuencia autenticación
            //<<<<<<>>>>>>>>>>
            error_name = "Autenticacion_consulta";
            error_user = "Error al validar autenticación";
            error_pdp = "Error respuesta: Autenticación inválida";

            string id_trx = "0";
            application = application.ToUpper();
            res_flujo = new Dictionary<string, dynamic>()
            {
                { "application", application},
                { "id_terminal", id_terminal},
                { "id_usuario", id_usuario},
                { "nombre_usuario", nombre_usuario},
            };
            error_msg = new Dictionary<string, dynamic>()
            {
                { "name", error_name},
                { "blocking", true},
                { "context", $"Exception = "},
                { "description", error_user },
                { "error_pdp", error_pdp },
            };

            //if (User != null)
            //{
            //    if (!User.IsValid())
            //    {
            //        Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
            //        Out_fallida.totalValue = 0;
            //        Out_fallida.responseCode = "ER";
            //        Out_fallida.responseError.errorCode = "00008";
            //        Out_fallida.responseError.errorType = "B2B";
            //        Out_fallida.responseError.errorDescription = "Error de autenticación hacia el cliente";
            //        output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
            //        return Tuple.Create(output, error_msg, id_trx, res_flujo);
            //    }
            //}
            //else
            //{
            //    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
            //    Out_fallida.totalValue = 0;
            //    Out_fallida.responseCode = "ER";
            //    Out_fallida.responseError.errorCode = "00008";
            //    Out_fallida.responseError.errorType = "B2B";
            //    Out_fallida.responseError.errorDescription = "Error de autenticación hacia el cliente";
            //    output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
            //    return Tuple.Create(output, error_msg, id_trx, res_flujo);
            //}

            //Secuencia generar id trx 
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
                            {"obj",(input.contentXML.consultOfCollectionRequest.reference1 != "") ? Convert.ToInt64(referencia1) : (long?)null}
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
                output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
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
                if (constantes_bcs.ean.TryGetValue(input.contentXML.consultOfCollectionRequest.EANCode, out value_ean) is false)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Código EAN inválido");
                    Out_fallida.responseError.errorCode = "00006";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference2
                if (input.contentXML.consultOfCollectionRequest.reference2.Length == 0 || ContainsNonZeroCharacter(input.contentXML.consultOfCollectionRequest.reference2))
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Factura inválida");
                    Out_fallida.responseError.errorCode = "00010";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference3
                if (input.contentXML.consultOfCollectionRequest.reference3.Length == 0 || ContainsNonZeroCharacter(input.contentXML.consultOfCollectionRequest.reference3))
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Factura inválida");
                    Out_fallida.responseError.errorCode = "00010";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //day
                if (input.contentXML.consultOfCollectionRequest.day != "N" && input.contentXML.consultOfCollectionRequest.day != "A")
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Horario no permitido para realizar consultas");
                    Out_fallida.responseError.errorCode = "00002";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento contentXML
                //TransactionCode
                if (input.contentXML.consultOfCollectionRequest.transactionCode.Length == 0 || input.contentXML.consultOfCollectionRequest.transactionCode.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud TransactionCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //EANCode
                if (input.contentXML.consultOfCollectionRequest.EANCode.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud EANCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //officeCode
                if (input.contentXML.consultOfCollectionRequest.officeCode.Length == 0 || input.contentXML.consultOfCollectionRequest.officeCode.Length > 4)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud officeCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference1
                if (input.contentXML.consultOfCollectionRequest.reference1.Length == 0 || input.contentXML.consultOfCollectionRequest.reference1.Length > 24)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.responseDescription = "Error en longitud reference1";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference2
                if (input.contentXML.consultOfCollectionRequest.reference2.Length > 24)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.responseDescription = "Error en longitud reference2";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference3
                if (input.contentXML.consultOfCollectionRequest.reference3.Length > 24)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.responseDescription = "Error en longitud reference3";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //totalValue
                if (input.contentXML.consultOfCollectionRequest.totalValue.ToString().Length > 14)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en longitud totalValue";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
               
                if (!decimal.TryParse(input.contentXML.consultOfCollectionRequest.totalValue, out totalValue))
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en campo totalValue";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }

                //transactionDate
                if (input.contentXML.consultOfCollectionRequest.transactionDate.Length != 8)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en longitud transactionDate";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //transactionHour
                if (input.contentXML.consultOfCollectionRequest.transactionHour.Length != 6)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    Out_fallida.responseDescription = "Error en longitud transactionHour";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //expirationDate
                if (input.contentXML.consultOfCollectionRequest.expirationDate.Length != 8)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud expirationDate";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //URL
                if (input.contentXML.consultOfCollectionRequest.URL.Length == 0 || input.contentXML.consultOfCollectionRequest.URL.Length > 500)
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
                if (input.parametersXML.metaService.requestDate.Length != 17)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud requestDate";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (input.parametersXML.metaService.bankCode.Length == 0 || input.parametersXML.metaService.bankCode.Length > 4)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud bankCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (input.parametersXML.metaService.clientCode.Length == 0 || input.parametersXML.metaService.clientCode.Length > 16)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud clientCode";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //service
                if (input.parametersXML.metaService.service.Length == 0 || input.parametersXML.metaService.service.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud service";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementNo
                if (input.parametersXML.metaService.agreementNo.Length == 0 || input.parametersXML.metaService.agreementNo.Length > 20)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud agreementNo";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-key
                if (input.parametersXML.metaService.parameters.key.Length == 0 || input.parametersXML.metaService.parameters.key.Length > 10)
                {
                    Out_fallida.responseError = new DataConsultaRecaudoBCSResponseError();
                    errores_schema.Add("Error inesperado en la consulta");
                    Out_fallida.responseDescription = "Error en longitud parameters key";
                    Out_fallida.responseError.errorCode = "00011";
                    Out_fallida.totalValue = 0;
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-value
                if (input.parametersXML.metaService.parameters.value.Length  == 0 || input.parametersXML.metaService.parameters.value.Length > 30)
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
                output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
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
                output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
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
                        output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
                        return Tuple.Create(output, error_msg, id_trx, res_flujo);
                    }
                    else
                    {
                        Out_exitosa.responseCode = "OK";
                        Out_exitosa.responseDescription = "Operación Exitosa";
                        Out_exitosa.totalValue = totalValue;
                        output.transactionXML.parametersXML.consultOfColectionResponse = Out_exitosa;
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
                output.transactionXML.parametersXML.consultOfColectionResponse = Out_fallida;
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }
        }


        [OperationFlujo]
        public Tuple<notificacionRecaudoBCSResponse, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoNotificacionRecaudoBCS(transactionXMLNotificacionRequest _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            transactionXMLNotificacion input = _input.transactionXML;
            notificacionRecaudoBCSResponse output = new notificacionRecaudoBCSResponse();
            output.transactionXML = new transactionXMLNotResponse();
            output.transactionXML.parametersXML = new parametersXMLNotResponse();
            output.transactionXML.parametersXML.notificationOfCollectionResponse = new DataContractNotificacionRecaudoBCSResponse();
            output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError = new DataContractNotificacionRecaudoBCSResponseError();

            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionDate = input.contentXML.notificationOfCollectionRequest.paymentDate;
            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionCode = input.contentXML.notificationOfCollectionRequest.transactionCode;

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
            string id_comercio = input.contentXML.notificationOfCollectionRequest.reference1;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(input.contentXML.notificationOfCollectionRequest.reference1);
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
            if (decimal.TryParse(input.contentXML.notificationOfCollectionRequest.totalValue, out totalValue))
            {
                totalValue = totalValue / 100;
            }
            else 
            {
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en campo totalValue";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
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
                            {"Message",$"{output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription}"},
                            {"referencia1",(input.contentXML.notificationOfCollectionRequest.reference1 != "") ? Convert.ToInt64(referencia1) : (long?)null}
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                if (constantes_bcs.ean.TryGetValue(input.contentXML.notificationOfCollectionRequest.EANCode, out value_ean) is false)
                {
                    errores_schema.Add("Código EAN inválido");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00006";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference2
                if (input.contentXML.notificationOfCollectionRequest.reference2.Length == 0 || ContainsNonZeroCharacter(input.contentXML.notificationOfCollectionRequest.reference2))
                {
                    errores_schema.Add("Factura inválida");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00010";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //PaymentForm
                string value_payment_form;
                if (constantes_bcs.formaPago.TryGetValue(input.contentXML.notificationOfCollectionRequest.paymentForm, out value_payment_form) is false)
                {
                    errores_schema.Add("Forma de pago no válida");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00017";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue y totalvalue
                if (input.contentXML.notificationOfCollectionRequest.cashValue == "0" || input.contentXML.notificationOfCollectionRequest.totalValue == "0")
                {
                    errores_schema.Add("Valor de transacción no permitido");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00018";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento contentXML
                //TransactionCode
                if (input.contentXML.notificationOfCollectionRequest.transactionCode.Length != 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud TransactionCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementCode
                if (input.contentXML.notificationOfCollectionRequest.agreementCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.agreementCode.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud agreementCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementBankCode
                if (input.contentXML.notificationOfCollectionRequest.agreementBankCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.agreementBankCode.Length > 2)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud agreementBankCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (input.contentXML.notificationOfCollectionRequest.clientCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.clientCode.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud clientCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //EANCode
                if (input.contentXML.notificationOfCollectionRequest.EANCode.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud EANCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //accountNumber
                if (input.contentXML.notificationOfCollectionRequest.accountNumber.Length == 0 || input.contentXML.notificationOfCollectionRequest.accountNumber.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud accountNumber";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference1
                if (input.contentXML.notificationOfCollectionRequest.reference1.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud reference1";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference2
                if (input.contentXML.notificationOfCollectionRequest.reference2.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud reference2";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue
                decimal cashValue;
                if (!decimal.TryParse(input.contentXML.notificationOfCollectionRequest.cashValue, out cashValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en campo cashValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (input.contentXML.notificationOfCollectionRequest.cashValue.Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud cashValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //checkValue
                decimal checkValue;
                if (!decimal.TryParse(input.contentXML.notificationOfCollectionRequest.checkValue, out checkValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en campo checkValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (input.contentXML.notificationOfCollectionRequest.checkValue.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud checkValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //totalValue
                if (input.contentXML.notificationOfCollectionRequest.totalValue.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud totalValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentForm
                if (input.contentXML.notificationOfCollectionRequest.paymentForm.Length > 1)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud paymentForm";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentDate
                if (input.contentXML.notificationOfCollectionRequest.paymentDate.Length == 0 || input.contentXML.notificationOfCollectionRequest.paymentDate.Length > 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud paymentDate";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //officeCode
                if (input.contentXML.notificationOfCollectionRequest.officeCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.officeCode.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud officeCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (input.contentXML.notificationOfCollectionRequest.bankCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.bankCode.Length > 3)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud bankCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //isBatch
                if (input.contentXML.notificationOfCollectionRequest.isBatch != "true" && input.contentXML.notificationOfCollectionRequest.isBatch != "false")
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en el campo isBatch";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //dealCode
                if (input.contentXML.notificationOfCollectionRequest.dealCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.dealCode.Length > 50)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud dealCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento parametersXML
                //requestDate
                if (input.parametersXML.metaService.requestDate.Length != 17)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud requestDate";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (input.parametersXML.metaService.bankCode.Length == 0 || input.parametersXML.metaService.bankCode.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud bankCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (input.parametersXML.metaService.clientCode.Length == 0 || input.parametersXML.metaService.clientCode.Length > 16)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud clientCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //service
                if (input.parametersXML.metaService.service.Length == 0 || input.parametersXML.metaService.service.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud service";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementNo
                if (input.parametersXML.metaService.agreementNo.Length == 0 || input.parametersXML.metaService.agreementNo.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud agreementNo";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-key
                if (input.parametersXML.metaService.parameters.key.Length == 0 || input.parametersXML.metaService.parameters.key.Length > 10)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud parameters key";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-value
                if (input.parametersXML.metaService.parameters.value.Length == 0 || input.parametersXML.metaService.parameters.value.Length > 30)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud parameters value";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (errores_schema.Count != 0)
                {
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }

                ////////////////////////////////// isReverse //////////////////////////////
                if (input.contentXML.notificationOfCollectionRequest.isReverse != "false")
                {
                    errores_schema.Add("Error inesperado en la notificación");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error campo isReverse, se espera false";
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription= $"{ex.Message}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "00004";
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

                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                        {"codigo confirmacion recaudo", input.contentXML.notificationOfCollectionRequest.dealCode},
                        {"valor_trx", totalValue.ToString().Replace(",",".")},
                        {"referencia 1", referencia1.ToString()},
                    };
                    queries_base query_helper = new queries_base();
                    List<Dictionary<string, dynamic>> result_duplicado = query_helper.ConsultarRecaudo("tbl_recaudo_integrado_bancos",data_search,application);
                    Console.WriteLine(result_duplicado);
                    if (result_duplicado.Count != 0)
                    {
                        output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ERDUPLICATED";
                        output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Duplicado en datos de recaudo";
                        output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                        {"codigo confirmacion recaudo",input.contentXML.notificationOfCollectionRequest.dealCode},
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }

            output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "OK";
            return Tuple.Create(output, error_msg, id_trx, res_flujo);
        }

        [OperationFlujo]
        public Tuple<notificacionRecaudoBCSResponse, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoReversoNotificacionRecaudoBCS(transactionXMLNotificacionRequest _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            transactionXMLNotificacion input = _input.transactionXML;
            notificacionRecaudoBCSResponse output = new notificacionRecaudoBCSResponse();
            output.transactionXML = new transactionXMLNotResponse();
            output.transactionXML.parametersXML = new parametersXMLNotResponse();
            output.transactionXML.parametersXML.notificationOfCollectionResponse = new DataContractNotificacionRecaudoBCSResponse();
            output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError = new DataContractNotificacionRecaudoBCSResponseError();

            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionDate = input.contentXML.notificationOfCollectionRequest.paymentDate;
            output.transactionXML.parametersXML.notificationOfCollectionResponse.transactionCode = input.contentXML.notificationOfCollectionRequest.transactionCode;

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
            string id_comercio = input.contentXML.notificationOfCollectionRequest.reference1;
            if (Int64.TryParse(id_comercio, NumberStyles.Integer, CultureInfo.InvariantCulture, out referencia1))
            {
                referencia1 = Convert.ToInt64(input.contentXML.notificationOfCollectionRequest.reference1);
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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

            if (decimal.TryParse(input.contentXML.notificationOfCollectionRequest.totalValue, out totalValue))
            {
                totalValue = totalValue / 100;
            }
            else
            {
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en campo totalValue";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                return Tuple.Create(output, error_msg, id_trx, res_flujo);
            }
            try
            {
                try
                {
                    Dictionary<string, dynamic> data_search = new Dictionary<string, dynamic>()
                    {
                        {"codigo confirmacion recaudo",input.contentXML.notificationOfCollectionRequest.dealCode},
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
                        output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00015";
                        output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
                        output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = "No existe transacción a reversar";
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                        {"Message",$"{output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription}"},
                        {"referencia1",(input.contentXML.notificationOfCollectionRequest.reference1 != "") ? Convert.ToInt64(referencia1) : (long?)null}
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
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                            output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "ERDUPLICATED";
                            output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Duplicado en datos de recaudo";
                            output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
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
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
            }

            //Secuencia Verificar los datos de entrada del soap
            //<<<<<<>>>>>>>>>>
            error_name = "error_input_data_service";
            error_user = "Error con los datos de entrada del servicio";
            error_pdp = "Error respuesta: (Error con los datos de entrada del servicio [reversarRecaudoBCS])";
            //<<<<<<>>>>>>>>>>
            try
            {
                List<string> errores_schema = new List<string>();
                //EANCode
                string value_ean;
                if (constantes_bcs.ean.TryGetValue(input.contentXML.notificationOfCollectionRequest.EANCode, out value_ean) is false)
                {
                    errores_schema.Add("Código EAN inválido");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "00006";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //Reference2
                if (input.contentXML.notificationOfCollectionRequest.reference2.Length == 0 || ContainsNonZeroCharacter(input.contentXML.notificationOfCollectionRequest.reference2))
                {
                    errores_schema.Add("Factura inválida");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "00010";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //PaymentForm
                string value_payment_form;
                if (constantes_bcs.formaPago.TryGetValue(input.contentXML.notificationOfCollectionRequest.paymentForm, out value_payment_form) is false)
                {
                    errores_schema.Add("Forma de pago no válida");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00017";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue y totalvalue
                if (input.contentXML.notificationOfCollectionRequest.cashValue == "0" || input.contentXML.notificationOfCollectionRequest.totalValue == "0")
                {
                    errores_schema.Add("Valor de transacción no permitido");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "00018";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento contentXML
                //TransactionCode
                if (input.contentXML.notificationOfCollectionRequest.transactionCode.Length != 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud TransactionCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementCode
                if (input.contentXML.notificationOfCollectionRequest.agreementCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.agreementCode.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud agreementCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementBankCode
                if (input.contentXML.notificationOfCollectionRequest.agreementBankCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.agreementBankCode.Length > 2)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud agreementBankCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (input.contentXML.notificationOfCollectionRequest.clientCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.clientCode.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud clientCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //EANCode
                if (input.contentXML.notificationOfCollectionRequest.EANCode.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud EANCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //accountNumber
                if (input.contentXML.notificationOfCollectionRequest.accountNumber.Length == 0 || input.contentXML.notificationOfCollectionRequest.accountNumber.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud accountNumber";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference1
                if (input.contentXML.notificationOfCollectionRequest.reference1.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud reference1";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //reference2
                if (input.contentXML.notificationOfCollectionRequest.reference2.Length > 24)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud reference2";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //cashValue
                decimal cashValue;
                if (!decimal.TryParse(input.contentXML.notificationOfCollectionRequest.cashValue, out cashValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en campo cashValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (input.contentXML.notificationOfCollectionRequest.cashValue.Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud cashValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //checkValue
                decimal checkValue;
                if (!decimal.TryParse(input.contentXML.notificationOfCollectionRequest.checkValue, out checkValue))
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en campo checkValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (input.contentXML.notificationOfCollectionRequest.checkValue.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud checkValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //totalValue
                if (input.contentXML.notificationOfCollectionRequest.totalValue.ToString().Length > 15)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud totalValue";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentForm
                if (input.contentXML.notificationOfCollectionRequest.paymentForm.Length > 1)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud paymentForm";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //paymentDate
                if (input.contentXML.notificationOfCollectionRequest.paymentDate.Length == 0 || input.contentXML.notificationOfCollectionRequest.paymentDate.Length > 8)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud paymentDate";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //officeCode
                if (input.contentXML.notificationOfCollectionRequest.officeCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.officeCode.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud officeCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (input.contentXML.notificationOfCollectionRequest.bankCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.bankCode.Length > 3)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud bankCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //isBatch
                if (input.contentXML.notificationOfCollectionRequest.isBatch != "true" && input.contentXML.notificationOfCollectionRequest.isBatch != "false")
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en el campo isBatch";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //dealCode
                if (input.contentXML.notificationOfCollectionRequest.dealCode.Length == 0 || input.contentXML.notificationOfCollectionRequest.dealCode.Length > 50)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud dealCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                /////// Longitudes del fragmento parametersXML
                //requestDate
                if (input.parametersXML.metaService.requestDate.Length != 17)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud requestDate";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //bankCode
                if (input.parametersXML.metaService.bankCode.Length == 0 || input.parametersXML.metaService.bankCode.Length > 4)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud bankCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //clientCode
                if (input.parametersXML.metaService.clientCode.Length == 0 || input.parametersXML.metaService.clientCode.Length > 16)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud clientCode";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //service
                if (input.parametersXML.metaService.service.Length == 0 || input.parametersXML.metaService.service.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud service";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //agreementNo
                if (input.parametersXML.metaService.agreementNo.Length == 0 || input.parametersXML.metaService.agreementNo.Length > 20)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud agreementNo";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-key
                if (input.parametersXML.metaService.parameters.key.Length == 0 || input.parametersXML.metaService.parameters.key.Length > 10)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud parameters key";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                //parameters-value
                if (input.parametersXML.metaService.parameters.value.Length == 0 || input.parametersXML.metaService.parameters.value.Length > 30)
                {
                    errores_schema.Add("Error inesperado en la consulta");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error en longitud parameters value";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }
                if (errores_schema.Count != 0)
                {
                    throw new SchemaCustomException($"{string.Join(", ", errores_schema)}");
                }

                ////////////////////////////////// isReverse //////////////////////////////
                if (input.contentXML.notificationOfCollectionRequest.isReverse != "true")
                {
                    errores_schema.Add("Error inesperado en la notificación");
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00011";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseDescription = "Error campo isReverse, se espera true";
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{ex.Message}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
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
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "00004";
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

                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
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
                            {"codigo confirmacion recaudo",input.contentXML.notificationOfCollectionRequest.dealCode},
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
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00001";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "GEN";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = $"{error_msg["error_pdp"]}";
                    output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorTechnicalDescription = $"{error_msg["description"]}, {error_msg["name"]}, {error_msg["context"]}, {error_msg["blocking"]}";
                    return Tuple.Create(output, error_msg, id_trx, res_flujo);
                }
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseCode = "OK";
            }
            else 
            {
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorCode = "00015";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorType = "B2B";
                output.transactionXML.parametersXML.notificationOfCollectionResponse.responseError.errorDescription = "No existe transacción a reversar";
            }
            return Tuple.Create(output, error_msg, id_trx, res_flujo);
        }
    }
}