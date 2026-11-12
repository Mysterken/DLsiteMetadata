using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DLsiteMetadata.Tests.TestSupport;

/// <summary>Answers requests from a script and records them, so no test touches the network.</summary>
public class StubHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _answers = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHttpHandler Respond(HttpStatusCode status, string body)
    {
        _answers.Enqueue(_ => new HttpResponseMessage(status)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        return this;
    }

    public StubHttpHandler Throw(Exception exception)
    {
        _answers.Enqueue(_ => throw exception);
        return this;
    }

    public HttpClient CreateClient()
    {
        return new HttpClient(this);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_answers.Count == 0) throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");

        return Task.FromResult(_answers.Dequeue()(request));
    }
}
