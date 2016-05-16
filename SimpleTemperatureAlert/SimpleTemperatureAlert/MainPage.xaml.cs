using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SimpleTemperatureAlert
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //A class which wraps the barometric sensor
        BMP280 BMP280;
        private const int LED_PIN = 5;
        private GpioPin gpiopin;
        private GpioPinValue pinValue;
        //private const int pin=5;
        private DispatcherTimer timer;
        static DeviceClient deviceClient;
        static string iotHubUri = "<Iot Hub Host URL>";
        static string deviceName = "<device Name/Id>";
        static string deviceKey = "<Key>";
        static string connectionString = "HostName=<iotHubUri>;DeviceId=<deviceName>;SharedAccessKey=<deviceKey>";
        public MainPage()
        {
            this.InitializeComponent();
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {

                deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceName, deviceKey), TransportType.Http1);

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(30);
                timer.Tick += Timer_Tick;

                //Initialized the GPIO Controller.
                InitGPIO();
                if (gpiopin != null)
                {
                    timer.Start();
                }
                //Create a new object for our barometric sensor class 
                BMP280 = new BMP280();
                //Initialize the sensor
                await BMP280.Initialize();

                ReceiveMessagesFromIoTHub();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }
        private void InitGPIO()
        {
            // get the GPIO controller
            var gpio = GpioController.GetDefault();

            // return an error if there is no gpio controller
            if (gpio == null)
            {
                Debug.WriteLine("There is no GPIO controller.");
                return;
            }
            else
            {
                gpiopin = gpio.OpenPin(LED_PIN);
                pinValue = GpioPinValue.High;
                gpiopin.Write(pinValue);
                gpiopin.SetDriveMode(GpioPinDriveMode.Output);

                Debug.WriteLine("GPIO pins initialized correctly.");
            }

        }



        private async Task<BMP280SensorData> ReadBMP280SensorData()
        {
            var sensorData = new BMP280SensorData();
            try
            {
                //Create a constant for pressure at sea level.
                //This is based on your local sea level pressure (Unit: Hectopascal)
                const float seaLevelPressure = 1013.25f;

                sensorData.Temperature = (int)Math.Ceiling(await BMP280.ReadTemperature());
                sensorData.Pressure = await BMP280.ReadPreasure();
                sensorData.Altitude = await BMP280.ReadAltitude(seaLevelPressure);


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }

            return sensorData;
        }


        private async void Timer_Tick(object sender, object e)
        {
            //Reading Temperature/Pressure/Altitude Information from BMP280 Sensor        
            var BMP280SensorData = await ReadBMP280SensorData();

            Debug.WriteLine("BMP280 Sensor data\nTemperature:{0}, \nPressure:{1}, \nAltitude:{2}",
                BMP280SensorData.Temperature, BMP280SensorData.Pressure, BMP280SensorData.Altitude);
            if (BMP280SensorData != null)
            {
                //Sending Message to IoT Hub
                SendDeviceToCloudMessagesAsync(BMP280SensorData);
            }
           
        }

        private async void SendDeviceToCloudMessagesAsync(BMP280SensorData bMP280SensorData)
        {
            SimpleTemperatureAlertData simpleTemperatureAlertData = new SimpleTemperatureAlertData()
            {
                DeviceId = deviceName,
                Time = DateTime.UtcNow.ToString("o"),
                RoomTemp =(int)Math.Ceiling(bMP280SensorData.Temperature),
                RoomPressure = bMP280SensorData.Pressure.ToString(),
                RoomAlt = bMP280SensorData.Altitude.ToString(),
               
            };
            var jsonString = JsonConvert.SerializeObject(simpleTemperatureAlertData);
            var jsonStringInBytes = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(jsonString));

            await deviceClient.SendEventAsync(jsonStringInBytes);
            Debug.WriteLine("{0} > Sending message: {1}", DateTime.UtcNow, jsonString);
        }

        private async void ReceiveMessagesFromIoTHub()
        {
            await ReceiveCloudToDeviceMessageAsync();
        }
        public async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
           // var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Amqp);

            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    pinValue = GpioPinValue.Low;
                    gpiopin.Write(pinValue);

                    var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    await deviceClient.CompleteAsync(receivedMessage);
                    return messageData;
                }
                else
                {
                    pinValue = GpioPinValue.High;
                    gpiopin.Write(pinValue);
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        
    }
}
