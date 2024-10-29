using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebServiceBancos.CServidorSoapBCS
{
    [DataContract(Namespace = "")]
    public class TransactionXML
    {
        [DataMember(IsRequired = true, Order = 1)]
        public string parametersXML { get; set; }

        [DataMember(IsRequired = true, Order = 2)]
        public string contentXML { get; set; }
    }

    [DataContract(Namespace = "")]
    public class ResponseMsgB2BXML
    {
        [DataMember]
        public TransactionXML payload { get; set; }
    }

    [DataContract(Namespace = "")]
    public class ErrorTransactionXML
    {
        [DataMember]
        public int ErrorCode { get; set; }

        [DataMember]
        public string ErrorType { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string ErrorDetail { get; set; }
    }
}