using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;
using DeviceId;
using DeviceId.Formatters;
using DeviceId.Encoders;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace samplecdc.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public  Dictionary<string, HeartbeatRecord> connectedDevices = new Dictionary<string, HeartbeatRecord>();

        public async Task Consume(IEnumerable<string> topics, ClientConfig config)
        {
            var consumerConfig = new ConsumerConfig(config);
            string deviceId = new DeviceIdBuilder()
                .AddMachineName().UseFormatter(new StringDeviceIdFormatter(new PlainTextDeviceIdComponentEncoder()))
                .AddUserName().UseFormatter(new StringDeviceIdFormatter(new PlainTextDeviceIdComponentEncoder()))
                .ToString().Replace(" ", String.Empty).ToLower();

            consumerConfig.ClientId = deviceId;
            consumerConfig.GroupId = "stratuscdcstatus_app_" + deviceId + Guid.NewGuid();
            consumerConfig.AutoOffsetReset = AutoOffsetReset.Latest;
            consumerConfig.EnableAutoCommit = true;


            CancellationTokenSource cts = new CancellationTokenSource();
           /* Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };*/

            int progress = 1;

            using (var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                 //.SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                 .Build())
            {
                consumer.Subscribe(topics);

                try
                {
                    while (true)
                    {
                        var record = consumer.Consume(cts.Token);

                        var heartbeatRecord = new HeartbeatRecord();
                        var parataKey = JsonSerializer.Deserialize<ParataKey>(record.Message.Key);

                        heartbeatRecord.CustomerId = parataKey.customerId;
                        heartbeatRecord.DeviceId = parataKey.deviceId;
                        heartbeatRecord.SiteId = parataKey.siteId;

                        heartbeatRecord.Tag = System.Text.Encoding.Default.GetString(record.Message.Headers[5].GetValueBytes());

                        heartbeatRecord.LastHeartBeat = TimeZoneInfo.ConvertTimeFromUtc(record.Message.Timestamp.UtcDateTime, TimeZoneInfo.Local);
                        if (!connectedDevices.ContainsKey(heartbeatRecord.DeviceId))
                        {
                            connectedDevices.Add(heartbeatRecord.DeviceId, heartbeatRecord);
                        }
                        else
                        {
                            connectedDevices[heartbeatRecord.DeviceId] = heartbeatRecord;
                        }

                        // if(record.IsPartitionEOF) {
                        //UpdateHeartbeatView();
                        // } else {
                        //     Console.Clear();
                        //     Console.WriteLine("Loading Heartbeats" + new String('.', progress));
                        //     progress++;
                        // }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ctrl-C was pressed.
                }
                finally
                {
                    consumer.Close();
                }
            }

            await Task.CompletedTask;
        }
        public async void OnGet()
        {
            var settings = new Dictionary<string, string>();
            settings.Add("bootstrap.servers", "pkc-lgwgm.eastus2.azure.confluent.cloud:9092");
            settings.Add("security.protocol", "sasl_ssl");
            settings.Add("sasl.mechanisms", "PLAIN");
            settings.Add("sasl.username", "D3CWGNLCM7UBUR64");
            settings.Add("sasl.password", "NM15GHUb0svAMNqbjBqWR5BJ37wSJ4+yldFCnCD4eBTb/aZvxiv1fSF8IR8X/uvA");

            ClientConfig config = new ClientConfig(settings);

            await Consume("cdc.atp.dbo.Heartbeat;cdc.max.dbo.Heartbeat".Split(';').AsEnumerable(), config);
        }
    }
}
