using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebServiceBancos.CServidorSoapBCS
{
    [DataContract(Namespace = "")]
    public class parameters
    {
        [DataMember(IsRequired = true)] public metaServi metaService;
    }

    [DataContract(Namespace = "")]
    public class metaServi
    {
        [DataMember(IsRequired = true, Order = 1)] public string requestDate;
        [DataMember(IsRequired = true, Order = 2)] public string bankCode;
        [DataMember(IsRequired = true, Order = 3)] public string clientCode;
        [DataMember(IsRequired = true, Order = 4)] public string service;
        [DataMember(IsRequired = true, Order = 5)] public string agreementNo;
        [DataMember(IsRequired = true, Order = 6)] public param parameters;
    }

    [DataContract(Namespace = "")]
    public class param
    {
        [DataMember(IsRequired = true)] public string key;
        [DataMember(IsRequired = true)] public string value;
    }

    [DataContract(Namespace = "")]
    public class content
    {
        [DataMember(IsRequired = true)] public consultOfCollection consultOfCollectionRequest;
    }


    [DataContract(Namespace = "")]
    public class consultOfCollection
    {
        [DataMember(IsRequired = true, Order = 1)] public string transactionCode;
        [DataMember(IsRequired = true, Order = 2)] public string EANCode;
        [DataMember(IsRequired = true, Order = 3)] public string officeCode;
        [DataMember(IsRequired = true, Order = 4)] public string reference1;
        [DataMember(IsRequired = true, Order = 5)] public string reference2;
        [DataMember(IsRequired = true, Order = 6)] public string reference3;
        [DataMember(IsRequired = true, Order = 7)] public string totalValue;
        [DataMember(IsRequired = true, Order = 8)] public string transactionDate;
        [DataMember(IsRequired = true, Order = 9)] public string transactionHour;
        [DataMember(IsRequired = true, Order = 10)] public string expirationDate;
        [DataMember(IsRequired = true, Order = 11)] public string day;
        [DataMember(IsRequired = true, Order = 12)] public string URL;
    }

    [DataContract(Namespace = "")]
    public class transactionXMLConsulta
    {
        [DataMember(IsRequired = true)] public parameters parametersXML;
        [DataMember(IsRequired = true)] public content contentXML;
    }

    //[DataContract(Namespace = "")]
    //public class transactionXMLResponse
    //{
    //    [DataMember(IsRequired = true)] public parametersXMLResponse parametersXML;
    //}

    //[DataContract(Namespace = "")]
    //public class parametersXMLResponse
    //{
    //    [DataMember(IsRequired = true)] public DataConsultaRecaudoBResponse consultOfColectionResponse { get; set; }
    //}

    //[DataContract(Namespace = "")]
    //[KnownType(typeof(DataContractConsultaRecaudoBCSResponse))]
    //[KnownType(typeof(DataContractConsultaRecaudoBCSResponseExitosa))]

    //public abstract class DataConsultaRecaudoBResponse { }

    [DataContract(Name = "consultOfCollectionResponse")]
    public class DataContractConsultaRecaudoBCSResponseExitosa
    {
        private string paymentReference_ = null;
        private string EANCode_ = null;
        private string reference1_ = null;
        private string reference2_ = null;
        private string expirationDate_ = null;
        private string paymentDate_ = null;
        private decimal totalValue_ = 0;
        private string paymentType_ = null;
        private string address_ = "";
        private string userName_ = "";
        private string responseCode_ = "ER";
        private string responseDescription_ = "";

        [DataMember(Order = 1)]
        public string paymentReference
        {
            get { return paymentReference_; }
            set { paymentReference_ = value; }
        }

        [DataMember(Order = 2)]
        public string EANCode
        {
            get { return EANCode_; }
            set { EANCode_ = value; }
        }

        [DataMember(Order = 3)]
        public string reference1
        {
            get { return reference1_; }
            set { reference1_ = value; }
        }

        [DataMember(Order = 4)]
        public string reference2
        {
            get { return reference2_; }
            set { reference2_ = value; }
        }

        [DataMember(Order = 5)]
        public string expirationDate
        {
            get { return expirationDate_; }
            set { expirationDate_ = value; }
        }

        [DataMember(Order = 6)]
        public string paymentDate
        {
            get { return paymentDate_; }
            set { paymentDate_ = value; }
        }

        [DataMember(Order = 7)]
        public decimal totalValue
        {
            get { return totalValue_; }
            set { totalValue_ = value; }
        }

        [DataMember(Order = 8)]
        public string paymentType
        {
            get { return paymentType_; }
            set { paymentType_ = value; }
        }

        [DataMember(Order = 9)]
        public string address
        {
            get { return address_; }
            set { address_ = value; }
        }

        [DataMember(Order = 10)]
        public string userName
        {
            get { return userName_; }
            set { userName_ = value; }
        }

        [DataMember(Order = 11)]
        public string responseCode
        {
            get { return responseCode_; }
            set { responseCode_ = value; }
        }

        [DataMember(Order = 12)]
        public string responseDescription
        {
            get { return responseDescription_; }
            set { responseDescription_ = value; }
        }

    }

    [DataContract(Name = "consultOfCollectionResponse")]
    public class DataContractConsultaRecaudoBCSResponse
    {
        private string paymentReference_ = null;
        private string EANCode_ = null;
        private string reference1_ = null;
        private string reference2_ = null;
        private string expirationDate_ = null;
        private string paymentDate_ = null;
        private decimal totalValue_ = 0;
        private string paymentType_ = null;
        private string address_ = "";
        private string userName_ = "";
        private string responseCode_ = "ER";
        private string responseDescription_ = "";


        [DataMember(Order = 1)]
        public string paymentReference
        {
            get { return paymentReference_; }
            set { paymentReference_ = value; }
        }

        [DataMember(Order = 2)]
        public string EANCode
        {
            get { return EANCode_; }
            set { EANCode_ = value; }
        }

        [DataMember(Order = 3)]
        public string reference1
        {
            get { return reference1_; }
            set { reference1_ = value; }
        }

        [DataMember(Order = 4)]
        public string reference2
        {
            get { return reference2_; }
            set { reference2_ = value; }
        }

        [DataMember(Order = 5)]
        public string expirationDate
        {
            get { return expirationDate_; }
            set { expirationDate_ = value; }
        }

        [DataMember(Order = 6)]
        public string paymentDate
        {
            get { return paymentDate_; }
            set { paymentDate_ = value; }
        }

        [DataMember(Order = 7)]
        public decimal totalValue
        {
            get { return totalValue_; }
            set { totalValue_ = value; }
        }

        [DataMember(Order = 8)]
        public string paymentType
        {
            get { return paymentType_; }
            set { paymentType_ = value; }
        }

        [DataMember(Order = 9)]
        public string address
        {
            get { return address_; }
            set { address_ = value; }
        }

        [DataMember(Order = 10)]
        public string userName
        {
            get { return userName_; }
            set { userName_ = value; }
        }

        [DataMember(Order = 11)]
        public string responseCode
        {
            get { return responseCode_; }
            set { responseCode_ = value; }
        }

        [DataMember(Order = 12)]
        public string responseDescription
        {
            get { return responseDescription_; }
            set { responseDescription_ = value; }
        }

        [DataMember(Order = 13)]
        public DataConsultaRecaudoBCSResponseError responseError
        {
            get; set;
        }

    }

    [DataContract(Namespace = "")]
    public class DataConsultaRecaudoBCSResponseError
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