namespace Centazio.Core.Misc;

/// <summary>
/// When logging is required for HttpClients, then simply create the client as follows:
/// ```new HttpClient(new LoggingHttpClientMessageHandler())```
/// </summary>
public class LoggingHttpClientMessageHandler(HttpMessageHandler? inner = null) : DelegatingHandler(inner ?? new HttpClientHandler()) {

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
    Log.Debug("Request:");
    Log.Debug(request.ToString());
    if (request.Content is not null) Log.Debug(await request.Content.ReadAsStringAsync());
    Log.Debug(String.Empty);

    var response = await base.SendAsync(request, cancellationToken);

    Log.Debug("Response:");
    Log.Debug(response.ToString());
    Log.Debug(await response.Content.ReadAsStringAsync());
    Log.Debug(String.Empty);

    return response;
  }

}