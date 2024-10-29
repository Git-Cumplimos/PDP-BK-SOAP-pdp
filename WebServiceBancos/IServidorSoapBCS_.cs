using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web.Services.Protocols;
using WebServiceBancos.CServidorSoapBCS;

namespace WebServiceBancos
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IServicioRecaudosBCS" en el código y en el archivo de configuración a la vez.

    [ServiceContract(Name = "ServicioRecaudosBCS")]
    public interface IServidorSoapBCS_
    {
        [OperationContract]
        public consultaRecaudoBCSResponse consultaRecaudoBCS(transactionXMLConsultaRequest input);

        [OperationContract]
        public notificacionRecaudoBCSResponse notificacionRecaudoBCS(transactionXMLNotificacionRequest input);

        [OperationContract]
        public notificacionRecaudoBCSResponse reversoNotificacionRecaudoBCS(transactionXMLNotificacionRequest input);

    }

    [MessageContract(IsWrapped = false)]
    public class transactionXMLConsultaRequest
    {
        [MessageHeader(Namespace ="security")]
        public string Username { get; set; }

        [MessageHeader(Namespace = "security")]
        public string Password { get; set; }

        [MessageBodyMember(Namespace="")]
        public transactionXMLConsulta transactionXML;
    }

    [MessageContract(IsWrapped = false)]
    public class consultaRecaudoBCSResponse
    {
        [MessageBodyMember(Namespace = "")]
        public transactionXMLResponse transactionXML;
    }

    [MessageContract(IsWrapped = false)]
    public class transactionXMLNotificacionRequest
    {
        [MessageHeader(Namespace = "security")]
        public string Username { get; set; }

        [MessageHeader(Namespace = "security")]
        public string Password { get; set; }

        [MessageBodyMember(Namespace = "nr")]
        public transactionXMLNotificacion transactionXML;
    }


    [MessageContract(IsWrapped = false)]
    public class notificacionRecaudoBCSResponse
    {
        [MessageBodyMember]
        public transactionXMLNotResponse transactionXML;
    }

}
