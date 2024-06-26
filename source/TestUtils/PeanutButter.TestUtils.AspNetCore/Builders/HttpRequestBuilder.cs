﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;

// ReSharper disable MemberCanBePrivate.Global
#if BUILD_PEANUTBUTTER_INTERNAL
using Imported.PeanutButter.TestUtils.AspNetCore.Fakes;
using Imported.PeanutButter.Utils;
using static Imported.PeanutButter.RandomGenerators.RandomValueGen;

namespace Imported.PeanutButter.TestUtils.AspNetCore.Builders;
#else
using PeanutButter.TestUtils.AspNetCore.Fakes;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable ConstantConditionalAccessQualifier

namespace PeanutButter.TestUtils.AspNetCore.Builders;
#endif

/// <summary>
/// Builds an http request
/// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
internal
#else
public
#endif
    class HttpRequestBuilder : RandomizableBuilder<HttpRequestBuilder, HttpRequest>
{
    internal static HttpRequestBuilder CreateWithNoHttpContext()
    {
        return new HttpRequestBuilder(noContext: true);
    }

    /// <inheritdoc />
    public HttpRequestBuilder()
        : this(noContext: false)
    {
    }

    /// <summary>
    /// Unless otherwise specified, this will be used when
    /// encoding json, eg when creating a JSON request from
    /// an object. Under the hood, it uses System.Text.Json.JsonSerializer
    /// with default options, which may or may not suit your workload.
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string DefaultJsonEncoder<T>(T value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Allows setting the default JSON encoder for all HttpRequestBuilder
    /// instances
    /// </summary>
    /// <param name="encoder"></param>
    public static void SetDefaultJsonEncoder(Func<object, string> encoder)
    {
        _defaultJsonEncoder = encoder;
    }

    private static Func<object, string> _defaultJsonEncoder;

    /// <summary>
    /// Sets up a JSON encoder to use on this instance of
    /// HttpRequestBuilder only, to be used when generating
    /// a request body as JSON from an object
    /// </summary>
    /// <param name="encoder"></param>
    public void WithJsonEncoder(Func<object, string> encoder)
    {
        _customJsonEncoder = encoder;
    }

    private Func<object, string> _customJsonEncoder;

    /// <summary>
    /// Default constructor: creates the builder with basics set up
    /// </summary>
    internal HttpRequestBuilder(bool noContext) : base(Actualize)
    {
        WithForm(FormBuilder.BuildDefault())
            .WithMethod(HttpMethod.Get)
            .WithScheme("http")
            .WithPath("/")
            .WithHost("localhost")
            .WithPort(80)
            .WithHeaders(HeaderDictionaryBuilder.BuildDefault())
            .WithCookies(
                RequestCookieCollectionBuilder
                    .Create()
                    .Build()
            ).WithPostBuild(
                request =>
                {
                    if (request.Cookies is FakeRequestCookieCollection fake)
                    {
                        fake.HttpRequest = request;
                    }
                }
            );
        if (!noContext)
        {
            WithHttpContext(
                () => HttpContextBuilder.Create()
                    .WithRequest(() => CurrentEntity)
                    .Build()
            );
        }
    }

    /// <summary>
    /// Constructs the fake http request
    /// </summary>
    /// <returns></returns>
    protected override HttpRequest ConstructEntity()
    {
        return new FakeHttpRequest();
    }

    /// <summary>
    /// Randomizes the output
    /// </summary>
    /// <returns></returns>
    public override HttpRequestBuilder Randomize()
    {
        return WithRandomMethod()
            .WithRandomScheme()
            .WithRandomPath()
            .WithRandomHost()
            .WithRandomPort()
            .WithRandomHeaders()
            .WithRandomCookies();
    }

    /// <summary>
    /// Sets the HttpContext.Connection.RemoteIpAddress value
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithRemoteAddress(string ipAddress)
    {
        return WithRemoteAddress(IPAddress.Parse(ipAddress));
    }

    /// <summary>
    /// Sets the HttpContext.Connection.RemoteIpAddress value
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithRemoteAddress(IPAddress address)
    {
        return With(
            o => o.HttpContext.Connection.RemoteIpAddress = address
        );
    }

    /// <summary>
    /// Adds a random form to the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomForm()
    {
        return WithForm(
            FormBuilder.BuildRandom()
        );
    }

    /// <summary>
    /// Selects a random http method for the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomMethod()
    {
        return WithMethod(GetRandomHttpMethod());
    }

    /// <summary>
    /// Selects a random scheme (http|https) for the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomScheme()
    {
        return WithScheme(GetRandomFrom(HttpSchemes));
    }

    /// <summary>
    /// Selects a random path for the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomPath()
    {
        return WithPath(GetRandomPath());
    }

    /// <summary>
    /// Selects a random hostname for the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomHost()
    {
        return WithHost(GetRandomHostname());
    }

    /// <summary>
    /// Selects a random port (80-10000) for the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomPort()
    {
        return WithPort(GetRandomInt(80, 10000));
    }

    /// <summary>
    /// Adds some random X- prefixed headers for the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomHeaders()
    {
        return WithRandomTimes(
            o => o.Headers[$"X-{GetRandomString(4, 8)}"] = GetRandomString()
        );
    }

    /// <summary>
    /// Adds some random cookies to the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomCookies()
    {
        return WithRandomTimes(
            o =>
            {
                if (o.Cookies is FakeRequestCookieCollection fake)
                {
                    fake[GetRandomString(4, 10)] = GetRandomString();
                }
            }
        );
    }

    private static readonly string[] HttpSchemes =
    [
        "http",
        "https"
    ];

    private static void Actualize(HttpRequest built)
    {
        WarnIf(built.HttpContext is null, "no HttpContext set");
        WarnIf(built.HttpContext?.Request is null, "no HttpContext.Request set");
    }

    /// <summary>
    /// Sets the body for the request. If possible, form elements
    /// are derived from the body.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithBody(string body)
    {
        return WithBody(Encoding.UTF8.GetBytes(body));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public HttpRequestBuilder WithJsonBody<T>(T data)
    {
        var stringContent = EncodeJson(data);
        return WithBody(stringContent);
    }

    private string EncodeJson<T>(T data)
    {
        var encoder = _customJsonEncoder ?? _defaultJsonEncoder ?? DefaultJsonEncoder;
        return encoder(data);
    }

    /// <summary>
    /// Sets the body for the request. If possible, form elements
    /// are derived from the body.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithBody(byte[] body)
    {
        return WithBody(new MemoryStream(body));
    }

    /// <summary>
    /// Sets the body for the request. If possible, form elements
    /// are derived from the body.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithBody(Stream body)
    {
        return With(
            o => o.Body = body
        );
    }

    /// <summary>
    /// Sets a cookie on the request. Will overwrite an existing cookie
    /// with the same name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithCookie(string name, string value)
    {
        return With(
            o =>
            {
                CookieUtil.GenerateCookieHeader(
                    new Dictionary<string, string>()
                    {
                        [name] = value
                    },
                    o,
                    overwrite: false
                );
            }
        );
    }

    /// <summary>
    /// Sets a bunch of cookies on the request. Will overwrite
    /// existing cookies with the same name. Will _not_ remove
    /// any other existing cookies.
    /// </summary>
    /// <param name="cookies"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public HttpRequestBuilder WithCookies(
        IDictionary<string, string> cookies
    )
    {
        return With(
            o =>
            {
                CookieUtil.GenerateCookieHeader(
                    cookies,
                    o,
                    overwrite: false
                );
            }
        );
    }

    /// <summary>
    /// Clears cookies on the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithNoCookies()
    {
        return With(
            o =>
            {
                o.Headers.Remove(CookieUtil.HEADER);
            }
        );
    }

    /// <summary>
    /// Sets the cookie collection on the request
    /// </summary>
    /// <param name="cookies"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithCookies(IRequestCookieCollection cookies)
    {
        return With(
            o => o.Cookies = cookies
        );
    }

    /// <summary>
    /// Clears headers on the request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithNoHeaders()
    {
        return With(
            o => o.Headers.Clear()
        );
    }

    /// <summary>
    /// Sets a header on the request. Any existing header with
    /// the same name is overwritten.
    /// </summary>
    /// <param name="header"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHeader(
        string header,
        string value
    )
    {
        return With(
            o => o.Headers[header] = value
        );
    }

    /// <summary>
    /// Sets a bunch of headers on the request. Existing cookies
    /// with the same names are overwritten. Other existing
    /// cookies are left intact.
    /// </summary>
    /// <param name="headers"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public HttpRequestBuilder WithHeaders(
        IDictionary<string, string> headers
    )
    {
        return With(
            o =>
            {
                if (headers is null)
                {
                    throw new ArgumentNullException(nameof(headers));
                }

                foreach (var kvp in headers)
                {
                    o.Headers[kvp.Key] = kvp.Value;
                }
            }
        );
    }

    /// <summary>
    /// Sets the header dictionary on the request
    /// </summary>
    /// <param name="headers"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHeaders(IHeaderDictionary headers)
    {
        return With<FakeHttpRequest>(
            o => o.SetHeaders(headers)
        );
    }

    /// <summary>
    /// Sets the query collection on the request
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithQuery(IQueryCollection query)
    {
        return With(
            o => o.Query = query
        );
    }

    /// <summary>
    /// Sets a query parameter on the request
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithQueryParameter(string key, string value)
    {
        return With(
            o => o.Query.As<FakeQueryCollection>()[key] = value
        );
    }

    /// <summary>
    /// Set multiple query parameters from a dictionary
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithQueryParameters(
        IDictionary<string, string> parameters
    )
    {
        return With(
            o =>
            {
                var query = o.Query.As<FakeQueryCollection>();
                foreach (var kvp in parameters)
                {
                    query[kvp.Key] = kvp.Value;
                }
            }
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithPath(string path)
    {
        return With(
            o => o.Path = new PathString(SanitisePath(path))
        );
    }

    private static string SanitisePath(string path)
    {
        path ??= "";
        return !path.StartsWith("/")
            ? $"/{path}"
            : path;
    }

    /// <summary>
    /// Sets the base path on the request
    /// </summary>
    /// <param name="basePath"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithBasePath(string basePath)
    {
        return With(
            o => o.PathBase = new PathString(SanitisePath(basePath))
        );
    }

    /// <summary>
    /// Sets the host on the path
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHost(HostString host)
    {
        return With(
            o => o.Host = host
        );
    }

    /// <summary>
    /// Sets the port on the request
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithPort(int port)
    {
        return With(
            o => o.Host = new HostString(o.Host.Host, port)
        );
    }

    /// <summary>
    /// Sets the host on the request
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHost(string host)
    {
        return With(
            o => o.Host = o.Host.Port is null
                ? new HostString(host)
                : new HostString(host, o.Host.Port.Value)
        );
    }

    /// <summary>
    /// Sets the query string on the request
    /// </summary>
    /// <param name="queryString"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithQueryString(
        string queryString
    )
    {
        return WithQueryString(new QueryString(queryString));
    }

    /// <summary>
    /// Sets the query string on the request
    /// </summary>
    /// <param name="queryString"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithQueryString(
        QueryString queryString
    )
    {
        return With(
            o => o.QueryString = queryString
        );
    }

    /// <summary>
    /// Sets the scheme on the request
    /// </summary>
    /// <param name="scheme"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithScheme(string scheme)
    {
        return With(
            o => o.Scheme = scheme
        ).With(o => o.Protocol = FakeHttpRequest.GuessProtocolFor(o.Scheme));
    }

    /// <summary>
    /// Sets the method on the request
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithMethod(HttpMethod method)
    {
        return WithMethod(method.ToString());
    }

    /// <summary>
    /// Sets the method on the request
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithMethod(string method)
    {
        return With(o => o.Method = (method ?? "get").ToUpper());
    }

    /// <summary>
    /// Sets the http context on the request
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHttpContext(
        HttpContext context
    )
    {
        return WithHttpContext(() => context);
    }

    /// <summary>
    /// Sets the http context accessor on the request
    /// </summary>
    /// <param name="accessor"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHttpContext(
        Func<HttpContext> accessor
    )
    {
        return With<FakeHttpRequest>(
            o => o.SetContextAccessor(accessor),
            nameof(FakeHttpRequest.HttpContext)
        );
    }

    /// <summary>
    /// Sets the form on the request
    /// </summary>
    /// <param name="formCollection"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithForm(IFormCollection formCollection)
    {
        return With(
            o => o.Form = formCollection
        );
    }

    /// <summary>
    /// Set a field on the form of the request
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithFormField(
        string key,
        string value
    )
    {
        return With(
            o => o.Form.As<FakeFormCollection>()[key] = value
        );
    }

    /// <summary>
    /// Sets a collection of fields on the form of the request
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public HttpRequestBuilder WithFormFields(
        IDictionary<string, string> fields
    )
    {
        var dict = fields ?? throw new ArgumentNullException(nameof(fields));
        return With(
            o =>
            {
                var form = o.Form.As<FakeFormCollection>();
                foreach (var kvp in dict)
                {
                    form[kvp.Key] = kvp.Value;
                }
            }
        );
    }

    /// <summary>
    /// Adds a file to the form of the request
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithFormFile(IFormFile file)
    {
        return With(
            o => o.Form.As<FakeFormCollection>()
                .Files.As<FakeFormFileCollection>()
                .Add(file)
        );
    }

    /// <summary>
    /// Adds a form file with string content and the provided name and filename
    /// </summary>
    /// <param name="content"></param>
    /// <param name="name"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithFormFile(
        string content,
        string name,
        string fileName
    )
    {
        return WithFormFile(
            Encoding.UTF8.GetBytes(content),
            name,
            fileName
        );
    }

    /// <summary>
    /// Adds a form file with binary content and the provided name and filename
    /// </summary>
    /// <param name="content"></param>
    /// <param name="name"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithFormFile(
        byte[] content,
        string name,
        string fileName
    )
    {
        return WithFormFile(
            new MemoryStream(content),
            name,
            fileName
        );
    }

    /// <summary>
    /// Set the full url for the request
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithUrl(string url)
    {
        return WithUrl(new Uri(url));
    }

    /// <summary>
    /// Set the full url for the request
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithUrl(Uri url)
    {
        return With(o => o.As<FakeHttpRequest>().SetUrl(url));
    }

    /// <summary>
    /// Adds a form file with text content and the provided name and filename
    /// </summary>
    /// <param name="content"></param>
    /// <param name="name"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithFormFile(
        Stream content,
        string name,
        string fileName
    )
    {
        return With(
            o => o.Form.Files.As<FakeFormFileCollection>()
                .Add(new FakeFormFile(content, name, fileName))
        );
    }

    /// <summary>
    /// Sets the Content-Type header for the request.
    /// Note that this shouldn't be necessary most of
    /// the time - automatic content type detection
    /// is provided for requests with forms or JSON bodies
    /// </summary>
    /// <param name="contentType"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithContentType(
        string contentType
    )
    {
        return With(
            o => o.ContentType = contentType
        );
    }

    /// <summary>
    /// Sets the origin header to be the root of the request's uri
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithSelfOrigin()
    {
        return With(
            o => o.Headers["Origin"] = o.FullUrl().ToString().UriRoot()
        );
    }

    /// <summary>
    /// Sets a random url for this request
    /// </summary>
    /// <returns></returns>
    public HttpRequestBuilder WithRandomUrl()
    {
        return WithRandomMethod()
            .WithRandomScheme()
            .WithRandomPath()
            .WithRandomHost()
            .WithRandomPort();
    }

    /// <summary>
    /// Sets the Origin header
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithOrigin(string origin)
    {
        return With(
            o => o.Headers["Origin"] = origin
        );
    }

    /// <summary>
    /// Facilitates easier http context mutations
    /// </summary>
    /// <param name="mutator"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHttpContextMutator(
        Action<FakeHttpContext> mutator
    )
    {
        return With(o => mutator(o.HttpContext.As<FakeHttpContext>()));
    }
}