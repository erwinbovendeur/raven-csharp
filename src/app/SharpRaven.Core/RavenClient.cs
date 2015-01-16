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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using SharpRaven.Data;
using SharpRaven.Logging;
using SharpRaven.Utilities;

namespace SharpRaven
{
    /// <summary>
    /// The Raven Client, responsible for capturing exceptions and sending them to Sentry.
    /// </summary>
    public class RavenClient : IRavenClient
    {
        public ISentryRequestFactory SentryRequestFactory { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenClient" /> class.
        /// </summary>
        /// <param name="dsn">The Data Source Name in Sentry.</param>
        /// <param name="sentryRequestFactory">The optional factory that will be used to create the <see cref="SentryRequest" /> that will be sent to Sentry.</param>
        public RavenClient(string dsn, ISentryRequestFactory sentryRequestFactory = null)
            : this(new Dsn(dsn), sentryRequestFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenClient" /> class.
        /// </summary>
        /// <param name="dsn">The Data Source Name in Sentry.</param>
        /// <param name="sentryRequestFactory">The optional factory that will be used to create the <see cref="SentryRequest" /> that will be sent to Sentry.</param>
        /// <exception cref="System.ArgumentNullException">dsn</exception>
        public RavenClient(Dsn dsn, ISentryRequestFactory sentryRequestFactory = null)
        {
            if (dsn == null)
                throw new ArgumentNullException("dsn");

            SentryRequestFactory = sentryRequestFactory ?? new DefaultSentryRequestFactory();

            CurrentDsn = dsn;
            Logger = SystemUtilFactory.Instance.Logger;
            Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Enable Gzip Compression?
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool Compression { get; set; }

        /// <summary>
        /// The Dsn currently being used to log exceptions.
        /// </summary>
        public Dsn CurrentDsn { get; private set; }

        /// <summary>
        /// Interface for providing a 'log scrubber' that removes 
        /// sensitive information from exceptions sent to sentry.
        /// </summary>
        public IScrubber LogScrubber { get; set; }

        /// <summary>
        /// The name of the logger. The default logger name is "root".
        /// </summary>
        public string Logger { get; set; }

        /// <summary>
        /// Gets or sets the timeout value in milliseconds for the HTTP communication with Sentry.
        /// </summary>
        /// <value>
        /// The number of milliseconds to wait before the request times out. The default is 5,000 milliseconds (5 seconds).
        /// </value>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Captures the <see cref="Exception" />.
        /// </summary>
        /// <param name="exception">The <see cref="Exception" /> to capture.</param>
        /// <param name="message">The optional messge to capture. Default: <see cref="Exception.Message" />.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="exception" />. Default: <see cref="ErrorLevel.Error" />.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="exception" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="exception" />.</param>
        /// <returns>
        /// The <see cref="SentryRequest.EventID" /> of the successfully captured <paramref name="exception" />, or <c>null</c> if it fails.
        /// </returns>
        public Task<string> CaptureException(Exception exception,
                                       SentryMessage message = null,
                                       ErrorLevel level = ErrorLevel.Error,
                                       IDictionary<string, string> tags = null,
                                       object extra = null)
        {
            return Send(SentryRequestFactory.Create(CurrentDsn.ProjectID, exception, message, level, tags, extra));
        }

        /// <summary>
        /// Captures the message.
        /// </summary>
        /// <param name="message">The message to capture.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="message" />. Default <see cref="ErrorLevel.Info" />.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="message" />.</param>
        /// <returns>
        /// The <see cref="SentryRequest.EventID" /> of the successfully captured <paramref name="message" />, or <c>null</c> if it fails.
        /// </returns>
        public Task<string> CaptureMessage(SentryMessage message,
                                     ErrorLevel level = ErrorLevel.Info,
                                     IDictionary<string, string> tags = null,
                                     object extra = null)
        {
            return Send(SentryRequestFactory.Create(CurrentDsn.ProjectID, message, level, tags, extra));
        }

        /// <summary>
        /// Sends the specified packet to Sentry.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>
        /// The <see cref="SentryRequest.EventID"/> of the successfully captured JSON packet, or <c>null</c> if it fails.
        /// </returns>
        public virtual async Task<string> Send(SentryRequest packet)
        {
            packet.Logger = Logger;

            var handler = new HttpClientHandler();

            using (var client = new HttpClient(handler, true) {Timeout = Timeout})
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, CurrentDsn.SentryUri))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Add("X-Sentry-Auth", PacketBuilder.CreateAuthenticationHeader(CurrentDsn));
                    request.Headers.UserAgent.Add(new ProductInfoHeaderValue(PacketBuilder.ProductName,
                        PacketBuilder.ProductVersion.ToString()));

                    var data = packet.ToString(Formatting.None);
                    if (LogScrubber != null)
                        data = LogScrubber.Scrub(data);

                    if (Compression && SystemUtilFactory.Instance.CanCompress)
                    {
                        handler.AutomaticDecompression = DecompressionMethods.Deflate;

                        MemoryStream ms = new MemoryStream();
                        GzipUtilFactory.Instance.Write(data, ms);
                        ms.Flush();
                        ms.Position = 0;
                        request.Content = new StreamContent(ms);
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/content-type");
                        request.Content.Headers.ContentEncoding.Add("gzip");
                    }
                    else
                    {
                        request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                    }

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    string s = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SentryResponse>(s).Id;
                }
            }
        }
    }
}