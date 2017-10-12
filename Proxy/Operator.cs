using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace SecretNest.Web.Proxy
{

    abstract class Operator
    {
        readonly static string addressKey = Properties.Settings.Default.AddressKey;
        readonly static string addressKeyEqual = addressKey + "=";
        readonly static string addressFormat = Properties.Settings.Default.AddressFormat;
        readonly static StringCollection copyHeadersFromRequest = Properties.Settings.Default.CopyHeadersFromRequest;

        protected HttpClient httpClient;
        protected HttpClientHandler httpClientHandler;

        public void Process(HttpContext context)
        {
            string httpMethod = context.Request.HttpMethod;
            if (httpMethod != "GET" && httpMethod != "POST")
            {
                return;
            }

            //Get address
            var requestAddress = context.Request[addressKey];
            var address = string.Format(addressFormat, requestAddress);
            string request = address;

            var getParams = context.Request.QueryString.ToString()
                .Split('&')
                .Where(i => !i.StartsWith(addressKeyEqual));
            var getParamsString = string.Join("&", getParams);

            if (!string.IsNullOrEmpty(getParamsString))
            {
                request += "?" + getParamsString;
            }

            //Prepare request
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod == "GET" ? HttpMethod.Get : HttpMethod.Post, request);
            foreach (var headerKey in copyHeadersFromRequest)
            {
                var headerValues = context.Request.Headers.GetValues(headerKey);
                httpRequestMessage.Headers.Add(headerKey, headerValues);
            }
            if (httpMethod == "POST")
            {
                httpRequestMessage.Content = new StreamContent(context.Request.InputStream);
            }

            //Get response
            HttpResponseMessage httpResponseMessage = httpClient.SendAsync(httpRequestMessage).Result;

            //Send response
            context.Response.StatusCode = (int)httpResponseMessage.StatusCode;
            foreach(var header in httpResponseMessage.Headers)
            {
                foreach (var value in header.Value)
                {
                    context.Response.Headers.Add(header.Key, value);
                }
            }
            httpResponseMessage.Content.CopyToAsync(context.Response.OutputStream).Wait();
        }
    }

    class OperatorWithoutCert : Operator
    {
        public OperatorWithoutCert()
        {
            httpClientHandler = new HttpClientHandler();
            httpClient = new HttpClient(httpClientHandler);
        }
    }

    class OperatorWithCert : Operator
    {

        public OperatorWithCert(X509Certificate clientCert)
        {
            WebRequestHandler handler = new WebRequestHandler();
            handler.ClientCertificates.Add(clientCert);
            httpClient = new HttpClient(handler);
            httpClientHandler = handler;

            //dotNet 4.6 is required for supporting TLS 1.2
            //handler should be disposed if you change the lifespan other than full life time.
        }
    }
}