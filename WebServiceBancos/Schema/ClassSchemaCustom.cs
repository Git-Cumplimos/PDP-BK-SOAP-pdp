using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServiceBancos
{
    public class ClassSchemaCustom
    {
        [Serializable]
        public class SchemaCustomException : Exception
        {
            public SchemaCustomException(string message)
                : base(message) { }
        }
    }
}