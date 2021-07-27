using Microsoft.Extensions.Options;
using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class CallBackClient : ICallBackClient
    {
        private readonly HttpClient httpClient;
        private const string QUEUE_API_VERSION = "2020-10-02";
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;

        public CallBackClient(HttpClient httpClient,
                              IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.httpClient = httpClient;
            this.storageConfig = storageConfig;
        }

        public async Task CallBackApi(HttpMethod method, string requestBody, string uri)
        {
            HttpContent content = null;

            if (requestBody != null)
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = content } )
            {
                // Add the request headers for x-ms-date and x-ms-version.
                DateTime now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", QUEUE_API_VERSION);

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = GetAuthorizationHeader(storageConfig.Value.StorageAccountName, storageConfig.Value.StorageAccountKey, httpRequestMessage);

                // Send the request & return response.
                await httpClient.SendAsync(httpRequestMessage, CancellationToken.None).ConfigureAwait(false);
            }
        }
        public  AuthenticationHeaderValue GetAuthorizationHeader(string storageAccountName, string storageAccountKey, HttpRequestMessage httpRequestMessage)
        {
            // Raw representation of the message signature.
            HttpMethod method = httpRequestMessage.Method;
            string MessageSignature = CreateMessageSignature(storageAccountName, httpRequestMessage, method);

            //turn it into a byte array.
            byte[] SignatureBytes = Encoding.UTF8.GetBytes(MessageSignature);

            // Create the HMACSHA256 version of the storage key.
            HMACSHA256 SHA256 = new HMACSHA256(Convert.FromBase64String(storageAccountKey));

            // Compute the hash of the SignatureBytes and convert it to a base64 string.
            string signature = Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));

            AuthenticationHeaderValue authHeaderValue = new AuthenticationHeaderValue("SharedKey", storageAccountName + ":" + signature);
            return authHeaderValue;
        }

        private static string CreateMessageSignature(string storageAccountName, HttpRequestMessage httpRequestMessage, HttpMethod method)
        {
            return String.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n\n\n\n\n\n{7}{8}",
                      method.ToString(),
                      method != HttpMethod.Get && method != HttpMethod.Head && httpRequestMessage.Content?.Headers.ContentEncoding != null ? httpRequestMessage.Content.Headers.ContentEncoding.ToString() : string.Empty,
                      method != HttpMethod.Get && method != HttpMethod.Head && httpRequestMessage.Content?.Headers.ContentLanguage != null ? httpRequestMessage.Content.Headers.ContentLanguage.ToString() : string.Empty,
                      method != HttpMethod.Get && method != HttpMethod.Head ? httpRequestMessage.Content?.Headers.ContentLength.ToString() : string.Empty,
                      method != HttpMethod.Get && method != HttpMethod.Head && httpRequestMessage.Content?.Headers.ContentMD5 != null ? Convert.ToBase64String(httpRequestMessage.Content.Headers.ContentMD5) : string.Empty,
                      method != HttpMethod.Get && method != HttpMethod.Head && httpRequestMessage.Content?.Headers.ContentType != null ? httpRequestMessage.Content.Headers.ContentType.ToString() : string.Empty,
                      httpRequestMessage.Headers.Date != null ? httpRequestMessage.Headers.Date.Value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture) : string.Empty,
                      GetCanonicalizedHeaders(httpRequestMessage),
                      GetCanonicalizedResource(httpRequestMessage.RequestUri, storageAccountName)
                      );
        }

        /// <summary>
        /// Put the headers that start with x-ms in a list and sort them.
        /// Then format them into a string of [key:value\n] values concatenated into one string.
        /// (Canonicalized Headers = headers where the format is standardized).
        /// </summary>
        private static string GetCanonicalizedHeaders(HttpRequestMessage httpRequestMessage)
        {
            var headers = from kvp in httpRequestMessage.Headers
                          where kvp.Key.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase)
                          orderby kvp.Key
                          select new { Key = kvp.Key, kvp.Value };

            StringBuilder sb = new StringBuilder();

            // Create the string in the right format; this is what makes the headers "canonicalized" --
            //   it means put in a standard format. http://en.wikipedia.org/wiki/Canonicalization
            foreach (var kvp in headers)
            {
                StringBuilder headerBuilder = new StringBuilder(kvp.Key);
                char separator = ':';

                // Get the value for each header, strip out \r\n if found, then append it with the key.
                foreach (string headerValues in kvp.Value)
                {
                    string trimmedValue = headerValues.TrimStart().Replace("\r\n", String.Empty);
                    headerBuilder.Append(separator).Append(trimmedValue);

                    // Set this to a comma; this will only be used 
                    //   if there are multiple values for one of the headers.
                    separator = ',';
                }
                sb.Append(headerBuilder.ToString()).Append("\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// This part of the signature string represents the storage account 
        ///   targeted by the request. Will also include any additional query parameters/values.
        /// For ListContainers, this will return something like this:
        ///   /storageaccountname/\ncomp:list
        /// </summary>
        private static string GetCanonicalizedResource(Uri address, string storageAccountName)
        {
            // The absolute path is "/" because for we're getting a list of containers.
            StringBuilder sb = new StringBuilder("/").Append(storageAccountName).Append(address.AbsolutePath);

            // Address.Query is the resource, such as "?comp=list".
            // Create a NameValueCollection e.g. above will have entry key=comp, value=list.
            NameValueCollection values = HttpUtility.ParseQueryString(address.Query);

            foreach (var item in values.AllKeys.OrderBy(k => k))
            {
                sb.Append('\n').Append(item).Append(':').Append(values[item]);
            }

            return sb.ToString();
        }
    }
}