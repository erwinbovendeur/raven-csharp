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

using NUnit.Framework;

using SharpRaven.Utilities;

namespace SharpRaven.UnitTests.Utilities
{
    [TestFixture]
    public class PacketBuilderTests
    {
        [Test]
        public void CreateAuthenticationHeader_ReturnsCorrectAuthenticationHeader()
        {
            const string expected =
                @"^Sentry sentry_version=5, sentry_client=SharpRaven.Core/[\d\.]+, sentry_timestamp=\d+, sentry_key=74d3e971f0664ff0b4bfe3232b553a13, sentry_secret=59a9c4ef712d437c86ab3b48bf469685";

            var dsn = new Dsn(
                "https://74d3e971f0664ff0b4bfe3232b553a13:59a9c4ef712d437c86ab3b48bf469685@sentry.2face-it.nl/8");

            var authenticationHeader = PacketBuilder.CreateAuthenticationHeader(dsn);

            Assert.That(authenticationHeader, Is.StringMatching(expected));
        }
    }
}