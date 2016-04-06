﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using static PeanutButter.RandomGenerators.RandomValueGen;
using PeanutButter.SimpleTcpServer;
using PeanutButter.Utils;

namespace PeanutButter.SimpleHTTPServer.Tests
{
    [TestFixture]
    public class TestHttpServer
    {
        [Test]
        public void Construct_WhenPortIsSpecified_ShouldUseThatPort()
        {
            const int maxTries = 20;
            for (var i = 0; i < maxTries; i++)
            {
                var port = RandomValueGen.GetRandomInt(2000, 3000);
                try
                {
                    //---------------Set up test pack-------------------
                    using (var server = Create(port))
                    {
                        //---------------Assert Precondition----------------

                        //---------------Execute Test ----------------------

                        //---------------Test Result -----------------------
                        Assert.AreEqual(port, server.Port);
                    }
                    return;
                }
                catch (PortUnavailableException)
                {
                    Console.WriteLine($"Port {port} is currently not available");
                }
            }
            Assert.Fail($"Unable to bind to any specified port in {maxTries} attempts");
        }

        [Test]
        public void Construct_WhenPortIsNotSpecified_ShouldChooseRandomPortBetween_5000_And_50000()
        {
            //---------------Set up test pack-------------------
            for (var i = 0; i < 50; i++)
                using (var server = Create())
                {

                    //---------------Assert Precondition----------------

                    //---------------Execute Test ----------------------

                    //---------------Test Result -----------------------
                    Assert.That(server.Port, Is.GreaterThanOrEqualTo(5000));
                    Assert.That(server.Port, Is.LessThanOrEqualTo(50000));
                    Console.WriteLine("Server started on port: " + server.Port);
                }
        }

        [Test]
        public void Start_WhenConfiguredToServeXDocument_ShouldServeDocument()
        {
            //---------------Set up test pack-------------------
            var doc = "<html><head></head><body><p>" + RandomValueGen.GetRandomAlphaNumericString() + "</p></body></html>";
            const string theDocName = "index.html";
            using (var server = Create())
            {
                server.AddDocumentHandler((p, s) => p.Path == "/" + theDocName ? doc : null);
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var result = DownloadResultFrom(server, theDocName).ToUTF8String();

                //---------------Test Result -----------------------
                Assert.AreEqual(doc, result);
            }
        }

        public class SimpleData
        {
            public string SomeProperty { get; set; }
        }

        [Test]
        public void AddJsonDocumentHandler_ShouldServeJsonDocument()
        {
            //---------------Set up test pack-------------------
            var expected = GetRandomString();
            var data = new SimpleData { SomeProperty = expected };
            var route = "api/foo";
            using (var server = Create())
            {
                string contentType = null;
                server.AddJsonDocumentHandler((p, s) => p.Path == "/" + route ? data : null);
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------

                var stringResult = DownloadResultFrom(server, route, out contentType).ToUTF8String();

                //---------------Test Result -----------------------
                var resultAsObject = JsonConvert.DeserializeObject<SimpleData>(stringResult);
                Assert.IsNotNull(resultAsObject);
                Assert.AreEqual(expected, resultAsObject.SomeProperty);
            }
        }


        [Test]
        public void Start_WhenConfiguredToServeFile_ShouldReturnTheFileContents()
        {
            //---------------Set up test pack-------------------
            var theFile = RandomValueGen.GetRandomBytes(100, 200);
            const string theFileName = "somefile.bin";
            using (var server = Create())
            {
                //---------------Assert Precondition----------------
                server.AddFileHandler((p, s) => p.Path == "/" + theFileName ? theFile : null);
                //---------------Execute Test ----------------------
                var result = DownloadResultFrom(server, theFileName);
                //---------------Test Result -----------------------
                CollectionAssert.AreEqual(theFile, result);
            }
        }

        [Test]
        public void ServeDocument_GivenPathAndDocument_ShouldServeForThatPath()
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var doc = new XDocument(new XElement("html", new XElement("body", new XElement("p", new XText(RandomValueGen.GetRandomString())))));
                server.ServeDocument("/index.html", doc);

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                string contentType = null;
                var result = DownloadResultFrom(server, "/index.html", out contentType);

                //---------------Test Result -----------------------
                Assert.AreEqual(doc.ToString(), result.ToUTF8String());
                Assert.AreEqual("text/html", contentType);
            }
        }

        [TestCase(HttpMethods.Get)]
        [TestCase(HttpMethods.Post)]
        public void ServeDocument_GivenPathAndDocumentAndVerb_ShouldServeForThatPathAndDocumentAndVerb(HttpMethods serveMethod)
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var invalidMethod = serveMethod == HttpMethods.Get ? HttpMethods.Post : HttpMethods.Get;
                var doc = new XDocument(new XElement("html", new XElement("body", new XElement("p", new XText(RandomValueGen.GetRandomString())))));
                var path = "/index.html";
                server.ServeDocument(path, doc, serveMethod);

                //---------------Assert Precondition----------------
                Assert.AreNotEqual(serveMethod, invalidMethod);

                //---------------Execute Test ----------------------
                Console.WriteLine("Attempt to download path: " + path);
                string contentType = null;
                var ex = Assert.Throws<WebException>(() => DownloadResultFrom(server, invalidMethod, path, out contentType));
                var webResponse = ex.Response as HttpWebResponse;
                Assert.IsNotNull(webResponse, ex.Message);
                var statusCode = webResponse.StatusCode;
                Assert.AreEqual(HttpStatusCode.NotFound, statusCode);
                contentType = null;
                var result = DownloadResultFrom(server, serveMethod, path, out contentType);

                //---------------Test Result -----------------------
                Assert.AreEqual(doc.ToString(), result.ToUTF8String());
                Assert.AreEqual("text/html", contentType);
            }

        }

        [Test]
        public void ServeJsonDocument_GivenPathAndDocument_ShouldServeForThatPath()
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var obj = GetRandom<SimpleData>();
                server.ServeJsonDocument("/api/query", obj);

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                string contentType = null;
                var result = DownloadResultFrom(server, "/api/query", out contentType);

                //---------------Test Result -----------------------
                var resultAsObject = JsonConvert.DeserializeObject<SimpleData>(result.ToUTF8String());
                Assert.IsNotNull(result);
                Assert.AreEqual(obj.SomeProperty, resultAsObject.SomeProperty);
                Assert.AreEqual("application/json", contentType);
            }
        }

        [TestCase(HttpMethods.Get)]
        [TestCase(HttpMethods.Post)]
        public void ServeJsonDocument_GivenPathAndDocumentAndVerb_ShouldServeForThatPathAndVerbOnly(HttpMethods valid)
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var invalid = valid == HttpMethods.Get ? HttpMethods.Post : HttpMethods.Get;
                var obj = GetRandom<SimpleData>();
                var path = "/api/" + GetRandomString();
                server.ServeJsonDocument(path, obj, valid);

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                string contentType = null;
                Assert.Throws<WebException>(() => DownloadResultFrom(server, invalid, path, out contentType));
                contentType = null;
                var result = DownloadResultFrom(server, valid, path, out contentType);

                //---------------Test Result -----------------------
                var resultAsObject = JsonConvert.DeserializeObject<SimpleData>(result.ToUTF8String());
                Assert.IsNotNull(result);
                Assert.AreEqual(obj.SomeProperty, resultAsObject.SomeProperty);
                Assert.AreEqual("application/json", contentType);
            }
        }

        [Test]
        public void ServeDocument_GivenPathAndDocument_ShouldGive404ForOtherPaths()
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var doc = new XDocument(new XElement("html", new XElement("body", new XElement("p", new XText(RandomValueGen.GetRandomString())))));
                server.ServeDocument("/index.html", doc);

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var ex = Assert.Throws<WebException>(() => DownloadResultFrom(server, "/index1.html"));

                //---------------Test Result -----------------------
                var response = ex.Response as HttpWebResponse;
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Test]
        public void ServeFile_GivenPathAndData_ShouldServeForThatPath()
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var data = RandomValueGen.GetRandomBytes(10, 100);
                var contentType = "text/" + RandomValueGen.GetRandomFrom(new[] { "xml", "html", "javascript", "plain" });
                server.ServeFile("/file.bin", data, contentType);

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                string servedType = null;
                var result = DownloadResultFrom(server, "/file.bin", out servedType);

                //---------------Test Result -----------------------
                CollectionAssert.AreEqual(data, result);
                Assert.AreEqual(contentType, servedType);
            }
        }

        [Test]
        public void ServeFile_GivenPathAndData_ShouldGive404ForOtherPaths()
        {
            using (var server = Create())
            {
                //---------------Set up test pack-------------------
                var data = RandomValueGen.GetRandomBytes(10, 100);
                var contentType = "text/" + RandomValueGen.GetRandomFrom(new[] { "xml", "html", "javascript", "plain" });
                server.ServeFile("/file.bin", data, contentType);

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var ex = Assert.Throws<WebException>(() => DownloadResultFrom(server, "/file1.bin"));

                //---------------Test Result -----------------------
                var response = ex.Response as HttpWebResponse;
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Test]
        public void Download_GivenDownloadInfoAndOutputFolder_WhenDownloadUninterrupted_ShouldDownloadFileToOutputFolder()
        {
            using (var disposer = new AutoDisposer())
            {
                var deleter = disposer.Add(new AutoDeleter());
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var fileName = RandomValueGen.GetRandomAlphaNumericString(5, 10) + ".exe";
                deleter.Add(tempFolder);
                var expectedFile = Path.Combine(tempFolder, fileName);
                var server = Create();
                //---------------Set up test pack-------------------
                var url = server.GetFullUrlFor(fileName);
                var expectedBytes = RandomValueGen.GetRandomBytes(100, 200);
                server.AddFileHandler((processor, stream) =>
                {
                    if (processor.FullUrl == "/" + fileName)
                    {
                        return expectedBytes;
                    }
                    processor.WriteStatusHeader(404, "File not found");
                    return null;
                });

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                Download(url, fileName, tempFolder);

                //---------------Test Result -----------------------
                Assert.IsTrue(File.Exists(expectedFile));
                CollectionAssert.AreEquivalent(expectedBytes, File.ReadAllBytes(expectedFile));
            }
        }

        [Test]
        public void WhenNoHandlerClaimsPath_ShouldReturn404()
        {
            //---------------Set up test pack-------------------
            using (var server = Create())
            {

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var ex = Assert.Throws<WebException>(() => DownloadResultFrom(server, "/index.html"));

                //---------------Test Result -----------------------
                var response = ex.Response as HttpWebResponse;
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Test]
        public void WhenHandlerThrows_ShouldReturn500()
        {
            //---------------Set up test pack-------------------
            using (var server = Create())
            {
                var message = GetRandomString();
                var logs = new List<string>();
                server.LogAction = logs.Add;
                server.AddDocumentHandler((p,s) =>
                {
                    throw new Exception(message);
                });
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var ex = Assert.Throws<WebException>(() => DownloadResultFrom(server, "/index.html"));

                //---------------Test Result -----------------------
                var response = ex.Response as HttpWebResponse;
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.IsTrue(logs.Any(l => l.Contains(message)));
            }
        }



        private HttpServer Create(int? port = null)
        {
            var result = CreateWithPort(port);
            return result;
        }

        private HttpServer CreateWithPort(int? port)
        {
            return port.HasValue
                ? new HttpServer(port.Value, logAction: Console.WriteLine)
                : new HttpServer(logAction: Console.WriteLine);
        }

        private const string CONTENT_LENGTH_HEADER = "Content-Length";

        public string Download(string url, string fileName, string destPath)
        {
            var outFile = Path.Combine(destPath, fileName);
            var req = WebRequest.Create(url) as HttpWebRequest;
            long existingSize = 0;
            if (File.Exists(outFile))
            {
                var fullDownloadSize = GetContentLengthFor(url);
                var finfo = new FileInfo(outFile);
                existingSize = finfo.Length;
                if (fullDownloadSize == existingSize)
                {
                    Console.WriteLine("Already fully downloaded");
                    return outFile;
                }
                Console.WriteLine("Resuming download from byte: {0}", finfo.Length);
                req.AddRange(finfo.Length);
            }
            req.Timeout = 90000;
            using (var response = req.GetResponse())
            {
                var expectedSize = long.Parse(response.Headers[CONTENT_LENGTH_HEADER]);
                Console.WriteLine("Should get {0} bytes to {1}", expectedSize, outFile);
                DownloadFile(response, outFile, expectedSize, expectedSize + existingSize, existingSize);
                return outFile;
            }
        }

        private long GetContentLengthFor(string downloadUrl)
        {
            var req = WebRequest.Create(downloadUrl) as HttpWebRequest;
            using (var response = req.GetResponse())
            {
                return long.Parse(response.Headers[CONTENT_LENGTH_HEADER]);
            }
        }
        private void DownloadFile(WebResponse response, string outFile, long expectedSize, long totalSize, long offset)
        {
            var parentFolder = Path.GetDirectoryName(outFile);
            if (!Directory.Exists(parentFolder))
                Directory.CreateDirectory(parentFolder);
            using (var reader = new BinaryReader(response.GetResponseStream()))
            {
                using (var outStream = new FileStream(outFile, FileMode.Append))
                {
                    var haveRead = 0;
                    using (var writer = new BinaryWriter(outStream))
                    {
                        while (haveRead < expectedSize)
                        {
                            var toRead = expectedSize - haveRead;
                            if (toRead > 8192)
                                toRead = 8192;
                            var readBuf = reader.ReadBytes((int)toRead);
                            haveRead += readBuf.Length;
                            writer.Write(readBuf, 0, readBuf.Length);
                            writer.Flush();
                        }
                    }
                }
            }
        }

        private byte[] DownloadResultFrom(HttpServer server, string path)
        {
            string contentType;
            return DownloadResultFrom(server, path, out contentType);
        }

        private byte[] DownloadResultFrom(HttpServer server, HttpMethods method, string path, out string contentType)
        {
            var request = WebRequest.Create(server.GetFullUrlFor(path));
            request.Method = method.ToString().ToUpper();
            var response = request.GetResponse();
            const string contentTypeHeader = "Content-Type";
            var hasContentTypeHeader = response.Headers.AllKeys.Contains(contentTypeHeader);
            contentType = hasContentTypeHeader ? response.Headers[contentTypeHeader] : null;
            using (var s = response.GetResponseStream())
            {
                var memStream = new MemoryStream();
                s.CopyTo(memStream);
                return memStream.ToArray();
            }
        }

        private byte[] DownloadResultFrom(HttpServer server, string path, out string contentType)
        {
            return DownloadResultFrom(server, HttpMethods.Get, path, out contentType);
        }
    }

}
