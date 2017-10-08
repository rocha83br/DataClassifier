using System;
using System.Web;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace Rochas.DataClassifier.Helpers
{
    public static class RESTClient<T>
    {
        #region Declarations

        private static T result = default(T);
        private static string urlFormat = "{0}/{1}";
        private static string urlParamFormat = "{0}?{1}";
        private static HttpResponseMessage response = null;
        private static readonly string emptySvcRouteMsg = "Invalid service route";

        #endregion

        #region Public Async Methods

        public static async Task<T> Get(string serviceRoute, string parameters)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        var encodedParams = encodeParameterValues(parameters);
                        var serviceRouteParam = string.Format(urlParamFormat, serviceRoute, encodedParams);
                        response = await restCall.GetAsync(serviceRouteParam);

                        result = await response.Content.ReadAsAsync<T>();
                    }
                    else
                        throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return result;
        }

        public static async Task<bool> Post(string serviceRoute, T entity)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        response = await restCall.PostAsJsonAsync<T>(serviceRoute, entity);

                        return response.IsSuccessStatusCode;
                    }

                    throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static async Task<bool> Put(string serviceRoute, T entity)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        response = await restCall.PutAsJsonAsync<T>(serviceRoute, entity);

                        return response.IsSuccessStatusCode;
                    }
                    throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static async Task<bool> Delete(string serviceRoute, string parameters)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        var serviceRouteId = string.Format(urlParamFormat, serviceRoute, parameters);
                        response = await restCall.DeleteAsync(serviceRouteId);

                        return response.IsSuccessStatusCode;
                    }
                    throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        #endregion

        #region Public Sync Methods

        public static T GetSync(string serviceRoute, string parameters)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        var encodedParams = encodeParameterValues(parameters);
                        var serviceRouteParam = string.Format(urlParamFormat, serviceRoute, encodedParams);
                        response = restCall.GetAsync(serviceRouteParam).GetAwaiter().GetResult();

                        result = response.Content.ReadAsAsync<T>().GetAwaiter().GetResult();
                    }
                    else
                        throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return result;
        }

        public static bool PostSync(string serviceRoute, T entity)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        response = restCall.PostAsJsonAsync<T>(serviceRoute, entity).GetAwaiter().GetResult();

                        return response.IsSuccessStatusCode;
                    }
                    throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static bool PutSync(string serviceRoute, T entity)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        response = restCall.PutAsJsonAsync<T>(serviceRoute, entity).GetAwaiter().GetResult();

                        return response.IsSuccessStatusCode;
                    }
                    throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static bool DeleteSync(string serviceRoute, string parameters)
        {
            using (var restCall = new HttpClient())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRoute))
                    {
                        var serviceRouteId = string.Format(urlParamFormat, serviceRoute, parameters);
                        response = restCall.DeleteAsync(serviceRouteId).GetAwaiter().GetResult();

                        return response.IsSuccessStatusCode;
                    }
                    throw new InvalidOperationException(emptySvcRouteMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        #endregion

        #region Helper Methods

        private static string encodeParameterValues(string parameters)
        {
            StringBuilder preResult = new StringBuilder();
            var arrParams = parameters.Split('&');

            foreach (var param in arrParams)
            {
                var arrParamItem = param.Split('=');

                // Two-pass encode for special chars
                if (!arrParamItem[1].Contains("%"))
                    arrParamItem[1] = HttpUtility.UrlEncode(arrParamItem[1]);

                if (arrParamItem[1].Contains("+") || arrParamItem[1].Contains("/"))
                    arrParamItem[1] = arrParamItem[1].Replace("+", "%2b").Replace("/", "2f");

                preResult.Append(String.Join("=", arrParamItem));
                preResult.Append("&");
            }

            var result = preResult.ToString();

            if (result.Length > 1)
            {
                if (result.EndsWith("==&"))
                    return string.Concat(result.Substring(0, (result.Length - 3)), HttpUtility.UrlEncode("=="));
                else if (result.EndsWith("=&"))
                    return string.Concat(result.Substring(0, (result.Length - 2)), HttpUtility.UrlEncode("="));
                else
                    return result.Substring(0, (result.Length - 1));
            }
            else
                return null;
        }

        #endregion
    }
}
