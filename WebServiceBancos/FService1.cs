using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebServiceBancos.CServidorSoapBCS;

//using static WebServiceBancos.ClassSchemaCustom;
//using WebServiceBancos.Logs;
//using WebServiceBancos.templates_lib;
//using WebServiceBancos.ConexionBD;

namespace WebServiceBancos
{
    public class FService1
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

        public class OperationFlujo : Attribute
        {}

        public Tuple<responseMsgB2B, Dictionary<string, dynamic>, string, Dictionary<string, dynamic>> FlujoRecaudoBCS(requestMsgB2B _input, LoggerCustom OptionalLoggerCustom = null)
        {
            Dictionary<string, dynamic> res_flujo = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> error_msg = new Dictionary<string, dynamic>() { { "context", "" }, };

            TransactionXML input = _input.transactionXML;

            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new ResponseMsgB2BXML();

            return Tuple.Create(output, error_msg, "1", res_flujo);
        }

    }
}