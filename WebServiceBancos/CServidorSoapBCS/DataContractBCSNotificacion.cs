using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Serialization;

namespace WebServiceBancos.CServidorSoapBCS
{
    [DataContract(Namespace = "")]
    public class parametersNot
    {
        [DataMember(IsRequired = true)] public metaServiNot metaService;
    }

    [DataContract(Namespace = "")]
    public class metaServiNot
    {
        [DataMember(IsRequired = true, Order = 1)] public string requestDate;
        [DataMember(IsRequired = true, Order = 2)] public string bankCode;
        [DataMember(IsRequired = true, Order = 3)] public string clientCode;
        [DataMember(IsRequired = true, Order = 4)] public string service;
        [DataMember(IsRequired = true, Order = 5)] public string agreementNo;
        [DataMember(IsRequired = true, Order = 6)] public param parameters;
    }

    [DataContract(Namespace = "")]
    public class paramNot
    {
        [DataMember(IsRequired = true)] public string key;
        [DataMember(IsRequired = true)] public string value;
    }

    [DataContract(Namespace = "")]
    public class contentNot
    {
        [DataMember(IsRequired = true)] public notificationOfCollection notificationOfCollectionRequest;
    }


    [DataContract(Namespace = "")]
    public class notificationOfCollection
    {
        [DataMember(IsRequired = true, Order = 1)] public string transactionCode;
        [DataMember(IsRequired = true, Order = 2)] public string agreementCode;
        [DataMember(IsRequired = true, Order = 3)] public string agreementBankCode;
        [DataMember(IsRequired = true, Order = 4)] public string clientCode;
        [DataMember(IsRequired = true, Order = 5)] public string EANCode;
        [DataMember(IsRequired = true, Order = 6)] public string accountNumber;
        [DataMember(IsRequired = true, Order = 7)] public string reference1;
        [DataMember(IsRequired = true, Order = 8)] public string reference2;
        [DataMember(IsRequired = true, Order = 9)] public string cashValue;
        [DataMember(IsRequired = true, Order = 10)] public string checkValue;
        [DataMember(IsRequired = true, Order = 11)] public string totalValue;
        [DataMember(IsRequired = true, Order = 12)] public string paymentForm;
        [DataMember(IsRequired = true, Order = 13)] public string paymentDate;
        [DataMember(IsRequired = true, Order = 14)] public string officeCode;
        [DataMember(IsRequired = true, Order = 15)] public string bankCode;
        [DataMember(IsRequired = true, Order = 16)] public string isReverse;
        [DataMember(IsRequired = true, Order = 17)] public string isBatch;
        [DataMember(IsRequired = true, Order = 18)] public string dealCode;
    }

    [DataContract(Namespace = "")]
    public class transactionXMLNotificacion
    {
        [DataMember(IsRequired = true)] public parametersNot parametersXML;
        [DataMember(IsRequired = true)] public contentNot contentXML;
    }

    [DataContract(Namespace = "")]
    public class transactionXMLNotResponse
    {
        [DataMember(IsRequired = true)] public parametersXMLNotResponse parametersXML;
    }

    [DataContract(Namespace = "")]
    public class parametersXMLNotResponse
    {
        [DataMember(IsRequired = true)] public DataContractNotificacionRecaudoBCSResponse notificationOfCollectionResponse;
    }

    [DataContract(Namespace = "notificationOfCollectionResponse")]
    public class DataContractNotificacionRecaudoBCSResponse
    {
        private string transactionDate_ = "";
        private string transactionCode_ = "";
        private string responseCode_ = "ER";
        private string responseDescription_ = "";

        [DataMember(Order = 1)]
        public string transactionDate
        {
            get { return transactionDate_; }
            set { transactionDate_ = value; }
        }

        [DataMember(Order = 2)]
        public string transactionCode
        {
            get { return transactionCode_; }
            set { transactionCode_ = value; }
        }

        [DataMember(Order = 3)]
        public string responseCode
        {
            get { return responseCode_; }
            set { responseCode_ = value; }
        }

        [DataMember(Order = 4)]
        public string responseDescription
        {
            get { return responseDescription_; }
            set { responseDescription_ = value; }
        }

        [DataMember(Order = 5)]
        public DataContractNotificacionRecaudoBCSResponseError responseError 
        { 
            get; set; 
        }
    }

    [DataContract(Namespace = "")]
    public class DataContractNotificacionRecaudoBCSResponseError
    {
        private string errorCode_ = "";
        private string errorType_ = "";
        private string errorDescription_ = "";
        private string errorTechnicalDescription_ = "";

        [DataMember(Order = 1)]
        public string errorCode
        {
            get { return errorCode_; }
            set { errorCode_ = value; }
        }

        [DataMember(Order = 2)]
        public string errorType
        {
            get { return errorType_; }
            set { errorType_ = value; }
        }

        [DataMember(Order = 3)]
        public string errorDescription
        {
            get { return errorDescription_; }
            set { errorDescription_ = value; }
        }

        [DataMember(Order = 4)]
        public string errorTechnicalDescription
        {
            get { return errorTechnicalDescription_; }
            set { errorTechnicalDescription_ = value; }
        }
    }
}