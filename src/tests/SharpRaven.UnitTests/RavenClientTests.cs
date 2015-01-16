#region License

// Copyright (c) 2014 The Sentry Team and individual contributors.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
// 
//     1. Redistributions of source code must retain the above copyright notice, this list of
//        conditions and the following disclaimer.
// 
//     2. Redistributions in binary form must reproduce the above copyright notice, this list of
//        conditions and the following disclaimer in the documentation and/or other materials
//        provided with the distribution.
// 
//     3. Neither the name of the Sentry nor the names of its contributors may be used to
//        endorse or promote products derived from this software without specific prior written
//        permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;

using NUnit.Framework;

using SharpRaven.Data;
using SharpRaven.Logging;
using SharpRaven.UnitTests.Utilities;

namespace SharpRaven.UnitTests
{
    [TestFixture]
    public class RavenClientTests
    {
        private class TestableRavenClient : RavenClient
        {
            public TestableRavenClient(string dsn, ISentryRequestFactory sentryRequestFactory = null)
                : base(dsn, sentryRequestFactory)
            {
            }
  
            public override Task<string> Send(SentryRequest packet)
            {
                return Task.FromResult(packet.Project);
            }
        }

        private class TestableSentryRequestFactory : ISentryRequestFactory
        {
            private readonly string _project;
            private readonly ISentryRequestFactory _sentryRequestFactory;

            public TestableSentryRequestFactory(string project, ISentryRequestFactory sentryRequestFactory)
            {
                _project = project;
                _sentryRequestFactory = sentryRequestFactory;
            }

            public SentryRequest Create(string project, SentryMessage message, ErrorLevel level = ErrorLevel.Info, IDictionary<string, string> tags = null,
                object extra = null)
            {
                return _sentryRequestFactory.Create(_project, message, level, tags, extra);
            }

            public SentryRequest Create(string project, Exception exception, SentryMessage message = null, ErrorLevel level = ErrorLevel.Error,
                IDictionary<string, string> tags = null, object extra = null)
            {
                return _sentryRequestFactory.Create(_project, exception, message, level, tags, extra);
            }
        }

        [Test]
        public void CaptureMessage_InvokesSend_AndJsonPacketFactoryOnCreate()
        {
            const string dsnUri =
                "https://74d3e971f0664ff0b4bfe3232b553a13:59a9c4ef712d437c86ab3b48bf469685@sentry.2face-it.nl/8";
            var project = Guid.NewGuid().ToString();
            var jsonPacketFactory = new TestableSentryRequestFactory(project, new SentryRequestFactory());
            var client = new TestableRavenClient(dsnUri, jsonPacketFactory);
            var result = client.CaptureMessage("Test").Result;

            Assert.That(result, Is.EqualTo(project));
        }


        [Test]
        public void CaptureMessage_ScrubberIsInvoked()
        {
            string message = Guid.NewGuid().ToString("n");

            IRavenClient ravenClient = new RavenClient(TestHelper.DsnUri);
            ravenClient.LogScrubber = Substitute.For<IScrubber>();
            ravenClient.LogScrubber.Scrub(Arg.Any<string>())
                       .Returns(c =>
                       {
                           string json = c.Arg<string>();
                           Assert.That(json, Is.StringContaining(message));
                           return json;
                       });

            ravenClient.CaptureMessage(message).Wait();

            // Verify that we actually received a Scrub() call:
            ravenClient.LogScrubber.Received().Scrub(Arg.Any<string>());
        }

        [Test]
        public void Constructor_NullDsnString_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new RavenClient((string) null));
            Assert.That(exception.ParamName, Is.EqualTo("dsn"));
        }

        [Test]
        public void Constructor_NullDsn_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new RavenClient((Dsn) null));
            Assert.That(exception.ParamName, Is.EqualTo("dsn"));
        }

        [Test]
        public void Constructor_StringDsn_CurrentDsnEqualsDsn()
        {
            IRavenClient ravenClient = new RavenClient(TestHelper.DsnUri);
            Assert.That(ravenClient.CurrentDsn.ToString(), Is.EqualTo(TestHelper.DsnUri));
        }

        [Test]
        public void Logger_IsRoot()
        {
            IRavenClient ravenClient = new RavenClient(TestHelper.DsnUri);
            Assert.That(ravenClient.Logger, Is.EqualTo("root"));
        }
    }
}