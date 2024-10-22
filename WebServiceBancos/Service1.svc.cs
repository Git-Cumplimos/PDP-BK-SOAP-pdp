using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WebServiceBancos.CServidorSoapBCS;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace WebServiceBancos
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "Service1" en el código, en svc y en el archivo de configuración.
    // NOTE: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione Service1.svc o Service1.svc.cs en el Explorador de soluciones e inicie la depuración.
    [ServiceBehavior(Name = "ServiceBcscToB2BSync")]
    public class Service1 : FService1, IService1
    {
        [OperationBehavior]
        public responseMsgB2B InvokeSync(requestMsgB2B input)
        {
            responseMsgB2B output = new responseMsgB2B();
            output.transactionXML = new ResponseMsgB2BXML();
            
            faultServiceB2BException theFault = new faultServiceB2BException();
            try
            {
                Console.WriteLine(input.transactionXML.contentXML);
                XElement content = XElement.Parse(input.transactionXML.contentXML);
                Console.WriteLine(content);
                return output;
            }
            catch (Exception exp)
            {
                theFault.error.ErrorMessage = "Some Error " + exp.Message.ToString();
                throw new FaultException<faultServiceB2BException>(theFault);
            }
            
        }
    }
}
