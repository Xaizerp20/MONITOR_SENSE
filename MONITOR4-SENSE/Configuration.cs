using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MONITOR4
{
    internal class Configuration
    {
        public string HotelName { get; set; } = null!;
        public string MqttBroker { get; set; } = null!;
        public string MqttPort { get; set; } = null!;
        public string MqttClientName { get; set; } = null!;
        public string MqttTopic { get; set; } = null!;
        public int TurnosQuantity { get; set; }
        public int RoomsQuantity { get; set; }
        public string EmailHost { get; set; } = null!;
        public string EmailPort { get; set; } = null!;
        public string EmailRemitente { get; set; } = null!;
        public string EmailPassword { get; set; } = null!;
        public List<string> EmailDestinatarios { get; set; } = null!;
        public Dictionary<int, string> TimeTurnos { get; set; } = null!;
        public Dictionary<string, string> BoardConfig { get; set; } = null!;

        public Configuration()
        {

        }

        public async Task<Configuration?> getConfigurationData(string path)
        {
            try
            {
                string filePath = Path.Combine(path, "ServerConfiguration.json");
                Console.WriteLine("Path config: " + filePath);

                string jsonData = await File.ReadAllTextAsync(filePath);

                Configuration? confg = JsonSerializer.Deserialize<Configuration>(jsonData);

                return confg;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File Not found");
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Directory Not found");
                return null; 
            }

            

        }
    }
}
