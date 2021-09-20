using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using DeviceId;
using DeviceId.Formatters;
using DeviceId.Encoders;
using System.Threading;
using System.Text.Json;
using System.Linq;
using samplecdc.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace samplecdc
{
    public class HeartbeatData
    {
        private DataHub hub;
        public HeartbeatData(DataHub hubCtx)
        {
            hub = hubCtx;
        }

        public Dictionary<string, HeartbeatRecord> connectedDevices = new Dictionary<string, HeartbeatRecord>();

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

            using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
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

                        await hub.Clients.All.SendAsync("ReceiveDeviceList", JsonSerializer.Serialize(connectedDevices));
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
        public void GetData()
        {
            var settings = new Dictionary<string, string>();
            settings.Add("bootstrap.servers", "pkc-lgwgm.eastus2.azure.confluent.cloud:9092");
            settings.Add("security.protocol", "sasl_ssl");
            settings.Add("sasl.mechanisms", "PLAIN");
            settings.Add("sasl.username", "D3CWGNLCM7UBUR64");
            settings.Add("sasl.password", "NM15GHUb0svAMNqbjBqWR5BJ37wSJ4+yldFCnCD4eBTb/aZvxiv1fSF8IR8X/uvA");

            ClientConfig config = new ClientConfig(settings);

            Consume("cdc.atp.dbo.Heartbeat;cdc.max.dbo.Heartbeat".Split(';').AsEnumerable(), config);
        }
    }
}
