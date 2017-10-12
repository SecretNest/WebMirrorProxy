using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SecretNest.Web.Proxy
{
    /// <summary>
    /// Summary description for Proxy
    /// </summary>
    public class Proxy : IHttpHandler
    {


        public void ProcessRequest(HttpContext context)
        {
            Lazy<Operator> op = Operators.GetOne();
            try
            {
                op.Value.Process(context);
            }
            finally
            {
                Operators.PutOne(op);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}