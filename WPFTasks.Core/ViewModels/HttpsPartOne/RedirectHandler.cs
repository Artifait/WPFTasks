
using System.Net.Http;
using System.Net;

namespace WPFTasks.Core.ViewModels.HttpsPartOne
{
    public class RedirectHandler : DelegatingHandler
    {
        public RedirectHandler(HttpMessageHandler innerHandler) => InnerHandler = innerHandler;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseMessage = await base.SendAsync(request, cancellationToken);

            if (responseMessage is { StatusCode: HttpStatusCode.Redirect, Headers: { Location: { } } })
            {
                request = new HttpRequestMessage(HttpMethod.Get, responseMessage.Headers.Location);
                responseMessage = await base.SendAsync(request, cancellationToken);
            }

            return responseMessage;
        }
    }
}
