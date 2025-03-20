using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Serialization;

namespace WebServiceBancos.CServidorSoapBCS
{
    [DataContract]
    [XmlRoot(ElementName = "transactionXML")]
    public class transactionXML
    {
        [DataMember(IsRequired = true, Order = 1)]
        //[XmlElement(ElementName = "parametersXML", IsNullable = false)]
        public string parametersXML { get; set; }

        [DataMember(IsRequired = true, Order = 2)]
        //[XmlElement(ElementName = "contentXML", IsNullable = false)]
        public string contentXML { get; set; }
    }

    [DataContract]
    public class ResponseMsgB2BXML
    {
        [DataMember]
        public transactionXML transactionXML; //DUDA
    }

    [DataContract(Namespace = "http://com.bcsc.services.b2b")]
    public class errorTransactionXML
    {
        [DataMember]
        public int errorCode { get; set; }

        [DataMember]
        public string errorType { get; set; }

        [DataMember]
        public string errorMessage { get; set; }

        [DataMember]
        public string errorDetail { get; set; }
    }
}