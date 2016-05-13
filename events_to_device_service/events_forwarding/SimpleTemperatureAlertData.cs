using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace events_forwarding
{
    [DataContract]
    class SimpleTemperatureAlertData
    {
       

        [DataMember]
        public string Time { get; set; }

        [DataMember]
        public string DeviceId { get; set; }

        [DataMember]
        public int RoomTemp { get; set; }

        [DataMember]
        public string RoomPressure { get; set; }

       [DataMember]
        public string RoomAlt { get; set; }
    }
}
