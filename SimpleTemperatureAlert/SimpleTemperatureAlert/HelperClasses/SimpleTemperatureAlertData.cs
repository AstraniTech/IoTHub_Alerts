using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTemperatureAlert
{
    public class SimpleTemperatureAlertData
    {
        public string DeviceId { get; set; }

        public int RoomTemp { get; set; }

        public string RoomPressure { get; set; }

        public string RoomAlt { get; set; }
        public string Time { get; set; }

    }
}
