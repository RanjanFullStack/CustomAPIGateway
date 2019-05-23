using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace APIGateway
{
    public class Destination
    {
        public string Uri { get; set; }
        public bool RequiresAuthentication { get; set; }
        static HttpClient client = new HttpClient();
        public Destination(string uri, bool requiresAuthentication)
        {
            Uri = uri;
            RequiresAuthentication = requiresAuthentication;
        }

        public Destination(string path)
            : this(path, false)
        {
        }

        private Destination()
        {
            Uri = "/";
            RequiresAuthentication = false;
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequest request, bool requireAuth)
        {
            string requestContent = string.Empty, requestMethod, destinationUri;
            HttpResponseMessage httpResponseMessage = null;
            

            if (requireAuth)
            {
                requestMethod = HttpMethods.Get;
                destinationUri = Uri;
            }
            else
            {
                using (Stream receiveStream = request.Body)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        requestContent = readStream.ReadToEnd();
                    }
                }
                requestMethod = request.Method;
                destinationUri = CreateDestinationUri(request);
            }

            using (var newRequest = new HttpRequestMessage(new HttpMethod(requestMethod), destinationUri))
            {
                CopyHeaders(newRequest.Headers, request.Headers);
                newRequest.Content = new StringContent(requestContent, Encoding.UTF8, request.ContentType);
                httpResponseMessage = await client.SendAsync(newRequest);
            }

            return httpResponseMessage;
        }

        void CopyHeaders(HttpRequestHeaders to, IHeaderDictionary from)
        {
            foreach (string header in from.Keys)
                if (header == "Authorization")
                    to.Add(header, from[header].FirstOrDefault());
        }

        private string CreateDestinationUri(HttpRequest request)
        {
            var path = request.Path.ToString();
            string queryString = path.Count(x => x == '/') > 1 ? path.Split('/').Last() : "";
            return $"{Uri}{queryString}";
        }
    }
}