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
using NUnit.Framework;

using SharpRaven.Data;
using SharpRaven.UnitTests.Utilities;

namespace SharpRaven.UnitTests.Data
{
    [TestFixture]
    public class SentryRequestFactoryTests
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            this._sentryRequestFactory = new SentryRequestFactory();
        }

        #endregion

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

        private ISentryRequestFactory _sentryRequestFactory;

        [Test]
        public void TestNullProject()
        {
            var factory = new SentryRequestFactory();
            Assert.Throws<ArgumentNullException>(() => factory.Create(null, (SentryMessage)null));
        }

        [Test]
        public void TestNullException()
        {
            var factory = new SentryRequestFactory();
            Assert.Throws<ArgumentNullException>(() => factory.Create(null, (Exception)null));
        }

        [Test]
        public void Create_InvokesOnCreate()
        {
            var project = Guid.NewGuid().ToString("N");
            var factory = new TestableSentryRequestFactory(project, new SentryRequestFactory());
            var json = factory.Create(String.Empty, (SentryMessage) null);

            Assert.That(json.Project, Is.EqualTo(project));
        }


        [Test]
        public void Create_ProjectAndException_EventIDIsValidGuid()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, new Exception("Error"));

            Assert.That(json.EventID, Is.Not.Null.Or.Empty, "EventID");
            Assert.That(Guid.Parse(json.EventID), Is.Not.Null);
        }


        [Test]
        public void Create_ProjectAndException_MessageEqualsExceptionMessage()
        {
            var project = Guid.NewGuid().ToString();
            Exception exception = new Exception("Error");
            var json = this._sentryRequestFactory.Create(project, exception);

            Assert.That(json.Message, Is.EqualTo(exception.Message));
        }


        [Test]
        public void Create_ProjectAndException_ModulesHasCountGreaterThanZero()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, new Exception("Error"));

            Assert.That(json.Modules, Has.Count.GreaterThan(0));
        }


        [Test]
        public void Create_ProjectAndException_ProjectIsEqual()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, new Exception("Error"));

            Assert.That(json.Project, Is.EqualTo(project));
        }


        [Test]
        public void Create_ProjectAndException_ServerNameEqualsMachineName()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, new Exception("Error"));

            Assert.That(json.ServerName, Is.EqualTo(Environment.MachineName));
        }


        [Test]
        public void Create_Project_EventIDIsValidGuid()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, (SentryMessage) null);

            Assert.That(json.EventID, Is.Not.Null.Or.Empty, "EventID");
            Assert.That(Guid.Parse(json.EventID), Is.Not.Null);
        }


        [Test]
        public void Create_Project_ModulesHasCountGreaterThanZero()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, (SentryMessage) null);

            Assert.That(json.Modules, Has.Count.GreaterThan(0));
        }


        [Test]
        public void Create_Project_ProjectIsEqual()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, (SentryMessage) null);

            Assert.That(json.Project, Is.EqualTo(project));
        }


        [Test]
        public void Create_Project_ServerNameEqualsMachineName()
        {
            var project = Guid.NewGuid().ToString();
            var json = this._sentryRequestFactory.Create(project, (SentryMessage) null);

            Assert.That(json.ServerName, Is.EqualTo(Environment.MachineName));
        }

        [Test]
        public void TestSentryRequestCreation()
        {
            var factory = new SentryRequestFactory();
            var r = factory.Create("Test", TestHelper.GetException());
            Assert.IsTrue(r.Exceptions.Count == 1);

            var exception = r.Exceptions[0];
            Assert.True(string.Equals(exception.Module, "SharpRaven.UnitTests"));
            Assert.True(string.Equals(exception.Type, "DivideByZeroException"));
            Assert.True(string.Equals(exception.Value, "Attempted to divide by zero."));
            Assert.True(exception.ToString().StartsWith("DivideByZeroException: Attempted to divide by zero."));

            Assert.NotNull(exception.Stacktrace);

            var frames = exception.Stacktrace.Frames;
            Assert.True(frames.Length >= 1);

            if (frames.Length == 1)
            {
                // Release
                Assert.That(frames, Has.Length.EqualTo(1));
                Assert.That(frames[0].Function, Is.EqualTo("GetException"));
            }
            else
            {
                // Debug
                Assert.That(frames, Has.Length.EqualTo(2));
                Assert.That(frames[0].Function, Is.EqualTo("PerformDivideByZero"));
                Assert.That(frames[1].Function, Is.EqualTo("GetException"));
            }
        }
    }
}