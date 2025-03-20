using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml.Serialization;
using WebServiceBancos.CServidorSoapBCS;

namespace WebServiceBancos
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IService1" en el código y en el archivo de configuración a la vez.
    [ServiceContract(Name = "ServiceBcscToB2BSync", Namespace = "http://com.bcsc.services.b2b")]
    [XmlSerializerFormat]  // ¡Esto es clave para que respete la estructura XML!
    public interface IServidorSoapBCS_
    {
        [OperationContract]
        [FaultContract(typeof(errorTransactionXML), Name = "errorTransactionXML", Namespace = "http://com.bcsc.services.b2b")]
        responseMsgB2B invokeSync(requestMsgB2B request);

    }


    // Utilice un contrato de datos, como se ilustra en el ejemplo siguiente, para agregar tipos compuestos a las operaciones de servicio.
    [MessageContract(IsWrapped = false)]
    public class requestMsgB2B
    {

        [MessageBodyMember(Name = "payload")]
        [XmlElement(ElementName = "transactionXML", IsNullable = false)]
        public transactionXML transactionXML { get; set; }

        public void Validate()
        {
            if (transactionXML == null)
            {
                faultServiceB2BException theFault = new faultServiceB2BException
                {
                    error = new errorTransactionXML
                    {
                        errorCode = 1,
                        errorType = "GEN",
                        errorMessage = "transactionXML requerido",
                        errorDetail = "requestMsgB2B: transactionXML"
                    }
                };

                throw new FaultException<faultServiceB2BException>(theFault, new FaultReason(theFault.error.errorMessage));
            }
        }

    }

    [MessageContract(IsWrapped = false)]
    public class responseMsgB2B
    {
        [MessageBodyMember(Name = "payload")]
        [XmlElement(ElementName = "transactionXML")]
        public transactionXML transactionXML;
    }

    [MessageContract(IsWrapped = false)]
    public class faultServiceB2BException
    {
        [MessageBodyMember(Name = "error")]
        [XmlElement(ElementName = "errorTransactionXML")]
        public errorTransactionXML error { get; set; }
    }

    //[DataContract(Name = "faultServiceB2BException")]
    //public class faultServiceB2BException
    //{
    //    [DataMember(Name = "error", Order = 1)]
    //    public errorTransactionXML error { get; set; }
    //}

}
