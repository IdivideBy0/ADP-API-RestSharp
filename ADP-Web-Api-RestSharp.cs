using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Http;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;

namespace ADPRestSharp.Controllers
{

    public sealed class BearerToken
    {

        private DateTime _exp;

        public string _token;

        internal static byte[] ReadFile(string fileName)
        {
            FileStream f = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            int size = (int)f.Length;
            byte[] data = new byte[size];
            size = f.Read(data, 0, size);
            f.Close();
            return data;
        }

        public static string RequestBearerToken()
        {


            const string uriAccountsBase = "https://accounts.adp.com";

            const string clientId = "YOUR CLIENT ID";
            const string clientSecret = "YOUR CLIENT SECRET";

            const string grantType = "client_credentials";

            var client = new RestClient(uriAccountsBase + "/auth/oauth/v2/token?grant_type=" + grantType +
                "&client_id=" + clientId + "&client_secret=" + clientSecret);
            client.Timeout = -1;


            string certFile = HttpContext.Current.Server.MapPath(Path.Combine("~/Content/certificates/", "certfile.pfx"));

            byte[] rawData = ReadFile(certFile);

            X509Certificate2 certificate = new X509Certificate2(rawData, "YOUR CERT PASSWORD");

            client.ClientCertificates = new X509CertificateCollection() { certificate };

            var request = new RestRequest(Method.POST);

            IRestResponse response = client.Execute(request);


            string token = response.Content.Replace(Environment.NewLine, "").
                Replace("\"", "");

            int start = token.IndexOf("access_token:") + 13;
            int end = token.IndexOf(",");

            token = token.Substring(start, end - start);

            //return response.Content;
            return token;

        }


        // Singleton 
        private BearerToken()
        {
            _exp = DateTime.Now.AddSeconds(3600);
            _token = RequestBearerToken();
        }
        private static BearerToken instance = null;
        public static BearerToken Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BearerToken();
                }
                else if(instance._exp < DateTime.Now)
                {
                    instance = new BearerToken();
                }
                return instance;
            }
        }

    }
    public class ADPController : ApiController
    {
 
        public string RequestADP(string method)
        {

            string uriApiBase = "https://api.adp.com";
            string token = BearerToken.Instance._token;


            var client = new RestClient(uriApiBase + method);
            client.Timeout = -1;

            string certFile = HttpContext.Current.Server.MapPath(Path.Combine("~/Content/certificates/", "certfile.pfx"));

            byte[] rawData = BearerToken.ReadFile(certFile);

            X509Certificate2 certificate = new X509Certificate2(rawData, "adpadp10");

            client.ClientCertificates = new X509CertificateCollection() { certificate };


            var request = new RestRequest(Method.GET);
            request.AddHeader("Accept", "application/json;masked=false");
            request.AddHeader("Authorization", ("Bearer " + token));

            IRestResponse response = client.Execute(request);
            //Debug.WriteLine(response.Content);

            return response.Content;
        }
        

        // GET api/<controller>
        public string Get()
        {
            return RequestADP(@"/hr/v2/workers?$select=workers/person/legalName");

        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}