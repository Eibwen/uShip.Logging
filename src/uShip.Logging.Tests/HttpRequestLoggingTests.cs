﻿using FluentAssertions;
using log4net.Util;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using uShip.Logging.Extensions;

namespace uShip.Logging.Tests
{
    [TestFixture]
    public class HttpRequestLoggingTests
    {
        [Test]
        public void Should_use_current_context_if_no_context_passed_in()
        {
            var logFactory = Substitute.For<LogFactory>();
            var loggingEventDataBuilder = Substitute.For<LoggingEventDataBuilder>();
            var logger = new Logger(logFactory, loggingEventDataBuilder);
            SetHttpContext();

            logger
                .Message(string.Empty)
                .Response(Substitute.For<HttpResponseBase>())
                .Write();

            var properties = GetPropertiesDictionary(loggingEventDataBuilder);
            properties["Url"].Should().Be("http://www.example.com");
        }

        [Test]
        public void Should_use_request_from_context_when_passing_in_null()
        {
            var logFactory = Substitute.For<LogFactory>();
            var loggingEventDataBuilder = Substitute.For<LoggingEventDataBuilder>();
            var logger = new Logger(logFactory, loggingEventDataBuilder);
            SetHttpContext();

            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(new Uri("http://www.example.com/passed-in"));

            logger
                .Message(string.Empty)
                .Request(null)
                .Response(Substitute.For<HttpResponseBase>())
                .Write();

            var properties = GetPropertiesDictionary(loggingEventDataBuilder);
            properties["Url"].Should().Be("http://www.example.com");
        }

        [Test]
        public void Should_use_passed_in_request_if_not_null()
        {
            var logFactory = Substitute.For<LogFactory>();
            var loggingEventDataBuilder = Substitute.For<LoggingEventDataBuilder>();
            var logger = new Logger(logFactory, loggingEventDataBuilder);
            SetHttpContext();

            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(new Uri("http://www.example.com/passed-in"));

            logger
                .Message(string.Empty)
                .Request(requestBase)
                .Response(Substitute.For<HttpResponseBase>())
                .Write();

            var properties = GetPropertiesDictionary(loggingEventDataBuilder);
            properties["Url"].Should().Be("http://www.example.com/passed-in");
        }

        [Test]
        public void Should_log_appropriate_request_and_response_information()
        {
            var logFactory = Substitute.For<LogFactory>();
            var loggingEventDataBuilder = Substitute.For<LoggingEventDataBuilder>();
            var logger = new Logger(logFactory, loggingEventDataBuilder);

            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(new Uri("http://www.example.com"));
            requestBase.HttpMethod.Returns("GET");

            var responseBase = Substitute.For<HttpResponseBase>();
            responseBase.StatusCode.Returns(201);
            responseBase.Headers.Returns(new NameValueCollection {{"X-Test", "value"}});
            responseBase.OutputStream.Returns("body".ToStream());

            logger
                .Message(string.Empty)
                .Request(requestBase)
                .Response(responseBase)
                .Write();

            var properties = GetPropertiesDictionary(loggingEventDataBuilder);

            // Request
            properties["Url"].Should().Be("http://www.example.com");
            properties["RequestMethod"].Should().Be("GET");

            // Response
            properties["StatusCode"].Should().Be(201);
            properties["ResponseHeaders"].Should().Be("X-Test=value");
            properties["ResponseBody"].Should().Be("body");
        }

        [Test]
        public void Should_be_able_to_log_without_an_HttpContext()
        {
            var logFactory = Substitute.For<LogFactory>();
            var loggingEventDataBuilder = Substitute.For<LoggingEventDataBuilder>();
            var logger = new Logger(logFactory, loggingEventDataBuilder);

            logger
                .Message("Hello, World!")
                .Data("data-key", "data-value")
                .Write();

            var properties = GetPropertiesDictionary(loggingEventDataBuilder);

            properties["data-key"].Should().Be("data-value");
        }

        private static void SetHttpContext()
        {
            var request = new HttpRequest("filename", "http://www.example.com", string.Empty);
            var response = new HttpResponse(new StringWriter());
            var httpContext = new HttpContext(request, response);
            HttpContext.Current = httpContext;
        }

        private static PropertiesDictionary GetPropertiesDictionary(LoggingEventDataBuilder loggingEventDataBuilder)
        {
            return (PropertiesDictionary)loggingEventDataBuilder.ReceivedCalls().Single().GetArguments().Single(x => x is PropertiesDictionary);
        }
    }
}