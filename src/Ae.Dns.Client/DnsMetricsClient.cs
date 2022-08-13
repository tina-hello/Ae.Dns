﻿using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client which logs metrics aganst DNS responses.
    /// </summary>
    public sealed class DnsMetricsClient : IDnsClient
    {
        /// <summary>
        /// The name of the metrics meter.
        /// </summary>
        public static readonly string MeterName = "Ae.Dns.Client.DnsMetricsClient";

        /// <summary>
        /// The name of the successful response counter.
        /// </summary>
        public static readonly string SuccessCounterName = "Success";

        /// <summary>
        /// The name of the missing response (NXDomain) counter.
        /// </summary>
        public static readonly string MissingErrorCounterName = "MissingError";

        /// <summary>
        /// The name of the refused response counter.
        /// </summary>
        public static readonly string RefusedErrorCounterName = "RefusedError";

        /// <summary>
        /// The name of the other error response counter.
        /// </summary>
        public static readonly string OtherErrorCounterName = "OtherError";

        /// <summary>
        /// The name of the exception error response counter.
        /// </summary>
        public static readonly string ExceptionErrorCounterName = "ExceptionError";

        private static readonly Meter _meter = new Meter(MeterName);
        private static readonly Counter<int> _successCounter = _meter.CreateCounter<int>(SuccessCounterName);
        private static readonly Counter<int> _missingCounter = _meter.CreateCounter<int>(MissingErrorCounterName);
        private static readonly Counter<int> _refusedCounter = _meter.CreateCounter<int>(RefusedErrorCounterName);
        private static readonly Counter<int> _otherErrorCounter = _meter.CreateCounter<int>(OtherErrorCounterName);
        private static readonly Counter<int> _exceptionCounter = _meter.CreateCounter<int>(ExceptionErrorCounterName);

        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsMetricsClient"/> using the specified <see cref="IDnsClient"/>.
        /// </summary>
        public DnsMetricsClient(IDnsClient dnsClient) => _dnsClient = dnsClient;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default)
        {
            void IncrementCounter(Counter<int> counter) => counter.Add(1, new KeyValuePair<string, object>("Query", query));

            DnsAnswer answer;
            try
            {
                answer = await _dnsClient.Query(query, token);
            }
            catch
            {
                IncrementCounter(_exceptionCounter);
                throw;
            }

            switch (answer.Header.ResponseCode)
            {
                case DnsResponseCode.NoError:
                    IncrementCounter(_successCounter);
                    break;
                case DnsResponseCode.NXDomain:
                    IncrementCounter(_missingCounter);
                    break;
                case DnsResponseCode.Refused:
                    IncrementCounter(_refusedCounter);
                    break;
                default:
                    IncrementCounter(_otherErrorCounter);
                    break;
            }

            return answer;
        }
    }
}