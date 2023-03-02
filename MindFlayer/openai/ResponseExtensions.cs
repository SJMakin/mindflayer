﻿using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace OpenAI
{
    internal static class ResponseExtensions
    {
        private const string Organization = "Openai-Organization";
        private const string RequestId = "X-Request-ID";
        private const string ProcessingTime = "Openai-Processing-Ms";

        internal static void SetResponseData(this BaseResponse response, HttpResponseHeaders headers)
        {
            response.Organization = headers.GetValues(Organization).FirstOrDefault();
            response.RequestId = headers.GetValues(RequestId).FirstOrDefault();
            response.ProcessingTime = TimeSpan.FromMilliseconds(int.Parse(headers.GetValues(ProcessingTime).First()));
        }

        internal static async Task<string> ReadAsStringAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default, [CallerMemberName] string methodName = null)
        {
            var responseAsString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{methodName} Failed! HTTP status code: {response.StatusCode} | Response body: {responseAsString}");
            }

            return responseAsString;
        }
    }
}
