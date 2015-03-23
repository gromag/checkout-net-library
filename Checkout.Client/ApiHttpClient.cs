﻿using Checkout.ApiServices.SharedModels;
using Checkout.CommonLibraries.Services.PerfTracker;
using Checkout.Infrastructure;
using Checkout.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Checkout
{
    /// <summary>
    /// Handles http requests and responses
    /// </summary>
    public sealed class ApiHttpClient
    {
        private WebRequestHandler requestHandler;
        private HttpClient httpClient;
        private IPerfTrackerService perfTracker;
       // private static MediaTypeFormatter formatter;

        public ApiHttpClient()
        {
            perfTracker = new PerfTrackerService(true);
        //    formatter = new JsonNetFormatter();
            ResetHandler();
        }

        public void ResetHandler()
        {
            if (requestHandler != null)
            {
                requestHandler.Dispose();
            }
            requestHandler = new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                AllowAutoRedirect = false,
                UseDefaultCredentials = false,
                UseCookies = false
            };

            if (httpClient != null)
            {
                httpClient.Dispose();
            }

            httpClient = new HttpClient(requestHandler);
            httpClient.MaxResponseContentBufferSize = AppSettings.MaxResponseContentBufferSize;
            httpClient.Timeout = TimeSpan.FromSeconds(AppSettings.RequestTimeout);
            SetHttpRequestHeader("User-Agent",AppSettings.ClientUserAgentName);
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("Gzip"));
        }

        public void SetHttpRequestHeader(string name, string value)
        {
            if (httpClient.DefaultRequestHeaders.Contains(name))
            {
                httpClient.DefaultRequestHeaders.Remove(name);
            }

            if (value != null)
            { httpClient.DefaultRequestHeaders.Add(name, value); }
        }

        public string GetHttpRequestHeader(string name)
        {
            IEnumerable<string> values = null;
            httpClient.DefaultRequestHeaders.TryGetValues(name, out values);

            if (values != null && values.Any())
            { return values.First(); }

            return null;
        }

       
        /// <summary>
        /// Submits a get request to the given web address with default content type e.g. text/plain
        /// </summary>
        /// <param name="request">
        /// ApisEnum which holds the configuration for the given api. 
        /// </param>
        /// <param name="method">Http Method</param>
        /// <param name="content">Http body that is usually provided for Post and Put request</param>
        /// <param name="contentType">content type e.g. "application/json"</param>
        /// <returns></returns>
        public HttpResponse<T> GetRequest<T>(string requestUri,string authenticationKey)
        {
            var httpRequestMsg = new HttpRequestMessage();

            httpRequestMsg.Method = HttpMethod.Get;
            httpRequestMsg.RequestUri = new Uri(requestUri);
            httpRequestMsg.Headers.Add("Accept", AppSettings.DefaultContentType);

            SetHttpRequestHeader("Authorization", authenticationKey);

            if (AppSettings.DebugMode)
            {
                Console.WriteLine(string.Format("\n\n** Request ** Post {0}", requestUri));
            }

            return SendRequest<T>(httpRequestMsg).Result;
        }

        /// <summary>
        /// Submits a post request to the given web address
        /// </summary>
        /// <param name="api">
        /// ApisEnum which holds the configuration for the given api. 
        /// </param>
        /// <param name="method">Http Method</param>
        /// <param name="content">Http body content as json that is usually provided for Post and Put request</param>
        public HttpResponse<T> PostRequest<T>(string requestUri,string authenticationKey, object requestPayload = null)
        {
            var httpRequestMsg = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var requestPayloadAsString = GetObjectAsString(requestPayload);

            httpRequestMsg.Content = new StringContent(requestPayloadAsString, Encoding.UTF8, AppSettings.DefaultContentType);
            httpRequestMsg.Headers.Add("Accept", AppSettings.DefaultContentType);
            
            SetHttpRequestHeader("Authorization", authenticationKey);
            
            if (AppSettings.DebugMode)
            {
                Console.WriteLine(string.Format("\n\n** Request ** Post {0}", requestUri));
                Console.WriteLine(string.Format("\n\n** Payload ** \n {0} \n", requestPayloadAsString));
            }

            return SendRequest<T>(httpRequestMsg).Result;
        }

        /// <summary>
        /// Submits a put request to the given web address
        /// </summary>
        /// <param name="api">
        /// ApisEnum which holds the configuration for the given api. 
        /// </param>
        /// <param name="method">Http Method</param>
        /// <param name="content">Http body content as json that is usually provided for Post and Put request</param>
        public HttpResponse<T> PutRequest<T>(string requestUri, string authenticationKey, object requestPayload = null)
        {
            var httpRequestMsg = new HttpRequestMessage(HttpMethod.Put, requestUri);
            var requestPayloadAsString = GetObjectAsString(requestPayload);

            httpRequestMsg.Content = new StringContent(requestPayloadAsString, Encoding.UTF8, AppSettings.DefaultContentType);
            httpRequestMsg.Headers.Add("Accept", AppSettings.DefaultContentType);

            SetHttpRequestHeader("Authorization", authenticationKey);

            if (AppSettings.DebugMode)
            {
                Console.WriteLine(string.Format("\n\n** Request ** Put {0}", requestUri));
                Console.WriteLine(string.Format("\n\n** Payload ** \n {0} \n", requestPayloadAsString));
            }

            return SendRequest<T>(httpRequestMsg).Result;
        }

        /// <summary>
        /// Submits a delete request to the given web address
        /// </summary>
        /// <param name="api">
        /// ApisEnum which holds the configuration for the given api. 
        /// </param>
        /// <param name="method">Http Method</param>
        /// <param name="content">Http body content as json that is usually provided for Post and Put request</param>
        public HttpResponse<T> DeleteRequest<T>(string requestUri, string authenticationKey)
        {
            var httpRequestMsg = new HttpRequestMessage();

            httpRequestMsg.Method = HttpMethod.Delete;
            httpRequestMsg.RequestUri = new Uri(requestUri);
            httpRequestMsg.Headers.Add("Accept", AppSettings.DefaultContentType);

            SetHttpRequestHeader("Authorization", authenticationKey);

            if (AppSettings.DebugMode)
            {
                Console.WriteLine(string.Format("\n\n** Request ** Delete {0}", requestUri));
            }

            return SendRequest<T>(httpRequestMsg).Result; 
        }

        /// <summary>
        /// Sends a http request with the given object. All headers should be set manually here e.g. content type=application/json
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<HttpResponse<T>> SendRequest<T>(HttpRequestMessage request)
        {
            HttpResponse<T> response = null;
            HttpResponseMessage responseMessage = null;
            string responseAsString = null;

            var executionTime = new Stopwatch();
            executionTime.Start();
            string startTime = DateTime.Now.ToString();
            string responseCode = null;
            try
            {
                responseMessage = await httpClient.SendAsync(request); //ConfigureAwait(false).GetAwaiter().GetResult();
                executionTime.Stop();
                responseCode = responseMessage.StatusCode.ToString();

                var responseContent = responseMessage.Content.ReadAsByteArrayAsync().Result;

                if (responseContent != null && responseContent.Length > 0)
                {
                    responseAsString = Encoding.UTF8.GetString(responseContent);

                    if (AppSettings.DebugMode)
                    {
                        Console.WriteLine(string.Format("\n** HttpResponse - Status {0}**\n {1}\n", responseMessage.StatusCode, responseAsString));
                    }
                }

                response = CreateHttpResponse<T>(responseAsString, responseMessage.StatusCode);

                #region log
                //if (response.ContentType == HttpContentTypes.Xml || response.ContentType == HttpContentTypes.Json)
                //{
                //    if (HttpStatusCode.OK == response.HttpResponseStatusCode)
                //    {
                //        string responseContent;

                //        //handle Jsonp content
                //        var regex = new System.Text.RegularExpressions.Regex(@"\((.*)\)$");
                //        var match = regex.Match(response.HttpResponseAsString);
                //        if (match.Success && match.Value != string.Empty)
                //        {
                //            responseContent = match.Value;

                //            //Get rid of enclosing brackets
                //            responseContent = responseContent.StartsWith("(") ? responseContent.Substring(1, responseContent.Length - 1) : responseContent;
                //            responseContent = responseContent.EndsWith(")") ? responseContent.Substring(0, responseContent.Length - 1) : responseContent;

                //            response.HttpResponseAsString = responseContent;

                //            Console.WriteLine(string.Format("\n** HttpResponse is JsonP callback {0}**\n {1}\n", response.HttpResponseStatusCode, response.HttpResponseAsJObject.ToString()));
                //        }
                //        else
                //        {
                //            //Format output
                //            responseContent = response.ContentType == HttpContentTypes.Xml ?
                //                                        response.HttpResponseAsXmlDocument.XToIndentedString() :
                //                                        response.HttpResponseAsJObject.ToString();

                //            Console.WriteLine(string.Format("\n** HttpResponse {0}**\n {1}\n", response.HttpResponseStatusCode, responseContent));
                //        }


                //    }
                //    else
                //    {
                //        Console.WriteLine(string.Format("\n** HttpResponse {0}**\n {1}\n", response.HttpResponseStatusCode, response.HttpResponseAsString));
                //    }
                //}
                #endregion
            }
            catch (Exception ex)
            {
                executionTime.Stop();

                if (AppSettings.DebugMode)
                {
                    Console.WriteLine(string.Format(@"\n** Exception - HttpStatuscode:\n{0}**\n\n 
                        ** ResponseString {1}\n ** Exception Messages{2}\n ", (responseMessage != null ? responseMessage.StatusCode.ToString() : string.Empty), responseAsString, ExceptionHelper.FlattenExceptionMessages(ex)));
                }

                responseCode = "Exception" + ex.Message;

                throw;
            }
            finally
            {
                request.Dispose();
                ResetHandler();
                perfTracker.LogInformation(Thread.CurrentThread.ManagedThreadId.ToString(), request.RequestUri.ToString(), Convert.ToUInt32(executionTime.Elapsed.TotalMilliseconds), responseCode, startTime);
            }

            return response;
        }

        private HttpResponse<T> CreateHttpResponse<T>(string responseAsString, HttpStatusCode httpStatusCode)
        {
            if (httpStatusCode == HttpStatusCode.OK && responseAsString != null)
            {
                return new HttpResponse<T>(GetResponseAsObject<T>(responseAsString))
                {
                    HttpStatusCode = httpStatusCode
                };
            }
            else if (responseAsString != null)
            {
                return new HttpResponse<T>(default(T))
                {
                    Error = GetResponseAsObject<ResponseError>(responseAsString),
                    HttpStatusCode = httpStatusCode
                };
            }

            return null;
        }

        private string GetObjectAsString(object requestModel)
        {
            //if (AppSettings.DefaultContentType == HttpContentTypes.Json)
            //{
                return ContentAdaptor.ConvertToJsonString(requestModel);
            //}
            //else if (AppSettings.DefaultContentType == HttpContentTypes.Xml)
            //{
            //    return ContentAdaptor.ConvertToXmlString(requestModel);
            //}
            //else
            //{
            //    throw new ArgumentException("Content type not supported");
            //}
        }

        private T GetResponseAsObject<T>(string responseAsString)
        {
            //if (AppSettings.DefaultContentType == HttpContentTypes.Json)
            //{
                return ContentAdaptor.JsonStringToObject<T>(responseAsString);
            //}
            //else if (AppSettings.DefaultContentType == HttpContentTypes.Xml)
            //{
            //    return ContentAdaptor.XmlStringToObject<T>(responseAsString);
            //}
            //else
            //{
            //    throw new ArgumentException("Content type not supported");
            //}
        }

    }
}
