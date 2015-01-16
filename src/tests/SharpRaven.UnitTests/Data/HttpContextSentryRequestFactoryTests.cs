using System;
using System.Collections.Generic;
using NUnit.Framework;
using SharpRaven.Data;
using SharpRaven.UnitTests.Utilities;

namespace SharpRaven.UnitTests.Data
{
    [TestFixture]
    class HttpContextSentryRequestFactoryTests
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            // Set the HTTP Context to null before so tests don't bleed data into each other. @asbjornu
            HttpContextSentryRequestFactory.HttpContext = null;
        }


        [TearDown]
        public void TearDown()
        {
            // Set the HTTP Context to null before so tests don't bleed data into each other. @asbjornu
            HttpContextSentryRequestFactory.HttpContext = null;
        }

        #endregion

        private static void SimulateHttpRequest(Action<SentryHttpRequest> test)
        {
            using (var simulator = new HttpSimulator())
            {
                simulator.SetFormVariable("Form1", "Value1");
                simulator.SetHeader("UserAgent", "SharpRaven");
                simulator.SetCookie("Cookie", "Monster");

                using (simulator.SimulateRequest())
                {
                    var factory = new HttpContextSentryRequestFactory(new SentryRequestFactory());
                    var request = factory.Create("Test", "Hello");
                    test.Invoke(request.Request);
                }
            }
        }


        [Test]
        public void GetRequest_NoHttpContext_ReturnsNull()
        {
            var factory = new HttpContextSentryRequestFactory(new SentryRequestFactory());
            var request = factory.Create("Test", "Hello");
            Assert.That(request.Request, Is.Null);
        }


        [Test]
        [Category("NoMono")]
        public void GetRequest_WithHttpContext_RequestHasCookies()
        {
            SimulateHttpRequest(request =>
            {
                Assert.That(request.Cookies, Has.Count.EqualTo(1));
                Assert.That(request.Cookies["Cookie"], Is.EqualTo("Monster"));
            });
        }


        [Test]
        [Category("NoMono")]
        public void GetRequest_WithHttpContext_RequestHasFormVariables()
        {
            SimulateHttpRequest(request =>
            {
                Assert.That(request.Data, Is.TypeOf<Dictionary<string, string>>());

                var data = (Dictionary<string, string>)request.Data;

                Assert.That(data, Has.Count.EqualTo(1));
                Assert.That(data["Form1"], Is.EqualTo("Value1"));
            });
        }


        [Test]
        [Category("NoMono")]
        public void GetRequest_WithHttpContext_RequestHasHeaders()
        {
            SimulateHttpRequest(request =>
            {
                Assert.That(request.Headers, Has.Count.EqualTo(3));
                Assert.That(request.Headers["UserAgent"], Is.EqualTo("SharpRaven"));
            });
        }


        [Test]
        [Category("NoMono")]
        public void GetRequest_WithHttpContext_RequestIsNotNull()
        {
            SimulateHttpRequest(request => Assert.That(request, Is.Not.Null));
        }
    }
}
