using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONITOR4
{
    public class MQTT
    {
        IMqttClient mqttClient;
        private string broker { get; set; } = null!;
        private string clientId { get; set; } = null!;
        private string topic { get; set; } = null!;
        


    public async Task Connect_Client(string brokerIp, string clientId, Action<string> callback = null)
        {
            var mqttFactory = new MqttFactory();

            var options = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerIp, 1883)
            .WithClientId(clientId)
            .Build();

           mqttClient = mqttFactory.CreateMqttClient();


            mqttClient.ConnectedAsync += (async e =>
            {
                Console.WriteLine("MQTT connected");
                Console.WriteLine("");
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("my/topic/receive").Build());
            });

            mqttClient.ApplicationMessageReceivedAsync += (async e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                Console.WriteLine("Received MQTT message");
                Console.WriteLine($" - Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($" - Payload = {payload}");
                Console.WriteLine($" - QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($" - Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine("");

                callback?.Invoke(payload);
            });

            //reconection
            mqttClient.DisconnectedAsync += (async e =>
            {
                Console.WriteLine("Disconnected from MQTT broker. Trying to reconnect...");

                try
                {
                    await mqttClient.ReconnectAsync();

                    // Resubscribe to the topic after reconnection
                    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("zemiMonitor/Room").Build());

                    Console.WriteLine("Reconnected to MQTT broker successfully.");
                }
                catch (Exception ex)
                {
                    // Handle any exception that may occur during reconnection
                    Console.WriteLine($"Failed to reconnect to MQTT broker. Exception: {ex}");

                    // Wait for a while before attempting reconnection again
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Wait 5 seconds before retrying
                }
            });


            await mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        public async Task sendMessage(string message, string topic)
        {

            Console.WriteLine($"Publish MQTT message");
            Console.WriteLine($" - Topic: {topic}");
            Console.WriteLine($" - Payload: {message}");
            Console.WriteLine("");

            var applicationMessage = new MqttApplicationMessageBuilder()
              .WithTopic(topic)
              .WithPayload(message)
              .Build();
            await mqttClient.PublishAsync(applicationMessage);
        }

        public bool checkConnection()
        {
            return mqttClient.IsConnected;
        }

    }
}
