using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WebServiceBancos.CServidorSoapBCS;

namespace WebServiceBancos
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IService1" en el código y en el archivo de configuración a la vez.
    [ServiceContract(Name = "ServiceBcscToB2BSync")]
    public interface IService1
    {
        [OperationContract]
        [FaultContract(typeof(faultServiceB2BException), Name = "errorTransactionXML")]
        responseMsgB2B InvokeSync(requestMsgB2B request);
        // TODO: agregue aquí sus operaciones de servicio
    }


    // Utilice un contrato de datos, como se ilustra en el ejemplo siguiente, para agregar tipos compuestos a las operaciones de servicio.
    [MessageContract(IsWrapped = false)]
    public class requestMsgB2B
    {
        [MessageBodyMember(Namespace = "")]
        public TransactionXML transactionXML;
    }
    [MessageContract(IsWrapped = false)]
    public class responseMsgB2B
    {
        [MessageBodyMember(Namespace = "")]
        public ResponseMsgB2BXML transactionXML;
    }

    [MessageContract(IsWrapped = false)]
    public class faultServiceB2BException
    {
        [MessageBodyMember(Namespace = "")]
        public ErrorTransactionXML error { get; set; }
    }

}
