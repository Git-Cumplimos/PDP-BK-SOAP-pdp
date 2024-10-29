using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServiceBancos.CServidorSoapBCS
{
    public class ConstantesBCS
    {
        public Dictionary<string, string> ean = new Dictionary<string, string>()
        {
            { "7707232377896", "EAN1" },
        };

        public Dictionary<string, string> formaPago = new Dictionary<string, string>()
        {
            { "E", "Efectivo" },
            { "C", "Cheque" },
        };

    }
}