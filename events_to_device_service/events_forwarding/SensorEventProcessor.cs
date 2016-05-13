using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace events_forwarding
{
    class SensorEventProcessor : IEventProcessor
    {
        Stopwatch checkpointStopWatch;
        PartitionContext partitionContext;

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Trace.TraceInformation(string.Format("EventProcessor Shuting Down.  Partition '{0}', Reason: '{1}'.", this.partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        public Task OpenAsync(PartitionContext context)
        {
            Trace.TraceInformation(string.Format("Initializing EventProcessor: Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
            this.partitionContext = context;
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            Trace.TraceInformation("\n");
            Trace.TraceInformation("........ProcessEventsAsync........");
            //string commandParameterNew = "{\"Name\":\"AlarmThreshold\",\"Parameters\":{\"SensorId\":\"" + "Hello World" + "\"}}";
            //await WorkerRole.iotHubServiceClient.SendAsync("astranidevice", new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(commandParameterNew)));
            foreach (EventData eventData in messages)
            {
                try
                {
                    string jsonString = Encoding.UTF8.GetString(eventData.GetBytes());

                    Trace.TraceInformation(string.Format("Message received at '{0}'. Partition: '{1}'",
                        eventData.EnqueuedTimeUtc.ToLocalTime(), this.partitionContext.Lease.PartitionId));

                    Trace.TraceInformation(string.Format("-->Raw Data: '{0}'", jsonString));

                    var newSensorEvent = this.DeserializeEventData("[" + jsonString.Replace("}", "},").Remove(jsonString.Length - 1, 1) + "]");
                    // SimpleTemperatureAlertData newSensorEvent = Jso(jsonString);

                    //Trace.TraceInformation(string.Format("-->Serialized Data: '{0}', '{1}', '{2}', '{3}', '{4}'",
                    //    newSensorEvent.Time, newSensorEvent.RoomTemp, newSensorEvent.RoomPressure, newSensorEvent.RoomAlt, newSensorEvent.DeviceId));
                    foreach (var eventhubdata in newSensorEvent)
                    {
                        // Issuing alarm to device.
                        string commandParameterNew = "{\"Name\":\"Room Temperature\",\"Parameters\":{\"SensorId\":\"" + eventhubdata.RoomTemp + "\"}}";
                        Trace.TraceInformation("Issuing Eventhub data to device: '{0}', from sensor: '{1}'", eventhubdata.DeviceId, eventhubdata.RoomTemp);
                        Trace.TraceInformation("New Command Parameter: '{0}'", commandParameterNew);
                        await WorkerRole.iotHubServiceClient.SendAsync(eventhubdata.DeviceId, new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(commandParameterNew)));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Error in ProssEventsAsync -- {0}\n", ex.Message);
                }
            }

            await context.CheckpointAsync();
        }
        private SimpleTemperatureAlertData[] DeserializeEventData(string eventDataString)
        {
            return JsonConvert.DeserializeObject<SimpleTemperatureAlertData[]>(eventDataString);
        }

    }
}
