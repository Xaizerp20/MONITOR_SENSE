using System;
using System.Device.I2c;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Timers;
using System.Text;
using System.Security.Cryptography;
//using System.Text.Json;
using Newtonsoft.Json;
using System.Diagnostics;

namespace MONITOR4 // Note: actual namespace depends on the project name.
{
    internal class Program
    {


        public static BitArray[] SensorsGroups = new BitArray[16]; //grupo de bits por habitacion de la 1 a 64
        public static Room[] rooms; //habitaciones de la 1 a la 64
        public static I2cExpander[] expander = new I2cExpander[8]; //array de expanders de 0 a 7
        public static Dictionary<string, object> expanders = new Dictionary<string, object>(); //dicionary for store expanders with address
        public static Dictionary<int, Room> RoomDict = new Dictionary<int, Room>();


        private const byte PORT_SEL_REG_PORT0 = 0X06; //register to config port 0 how I/O
        private const byte PORT_SEL_REG_PORT1 = 0X07; //register to config port 1 how I/O
        private const byte INPUT_REG_PORT0 = 0X00; //register to read inputs values on  port 0
        private const byte INPUT_REG_PORT1 = 0X01;  //register to read inputs values on  port 1
        private const byte MUX_ADD = 0X70; //mux address default
        private static byte[] I2C_MUX_REGISTER_CHANNEL = new byte[] { 0x04, 0x05, 0x06, 0x07 }; //i2c register channels 0 - 3
        private static int i2cBus = 0;
        private static bool shouldStop = false;

        public static MQTT mqttServiceCloud = new MQTT();
        public static MQTT mqttServiceLocal = new MQTT();

        public static string mqttTopic = null!;

        static async Task Main(string[] args)
        {

            Configuration confg = await InitializeConfiguration();

            i2cBus = int.Parse(confg.BoardConfig["I2cBus"]);
            string cloudBroker = confg.BoardConfig["MqttCloudBroker"];
            string portCloud = confg.BoardConfig["MqttCloudPort"];
            string localBroker = confg.MqttBroker;
            string portLocal = confg.MqttPort;
            string clientName = confg.BoardConfig["MqttCloudClientName"];
            mqttTopic = confg.MqttTopic;
            int RoomsQuantity = confg.RoomsQuantity;
            rooms = new Room[RoomsQuantity];

            Console.WriteLine("Wait Configuring...");
            await Task.Delay(1000);

            //Console.WriteLine("Pulse Enter to Launch...");
            //Console.ReadKey();

            await mqttServiceCloud.Connect_Client(cloudBroker, clientName);
            await mqttServiceCloud.sendMessage("test mqtt c#", "test/zemiMonitor");

            await mqttServiceLocal.Connect_Client(localBroker, clientName);
            await mqttServiceLocal.sendMessage("test mqtt c#", "test/zemiMonitor");

            openMuxI2c(i2cBus, MUX_ADD, I2C_MUX_REGISTER_CHANNEL[2]); //open multiplexer i2c channel 2

            searchADD(i2cBus, true); //buscamos todos los dispositivos i2c conectados

            configExpandersInputs(); //reconfig

            createExpanders(i2cBus); //creacion de expanders en el bus 0

            CreateRooms(); //definimos la cantidad y creamos las habitaciones

            int i = 0;
            int j = 0;



            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (true) // Este bucle se ejecutará indefinidamente
            {



                Reader_ExpanderAsync();

                //sw.Stop(); // Detener la medición.
                //Console.WriteLine("Tiempo lectrua expander: {0}", sw.Elapsed.ToString("hh\\:mm\\:ss\\.fff"));

                if (i == 500)
                {
                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
                    //Console.WriteLine("timer: " + j);
                    Console.WriteLine("Elpased Time: {0}", sw.Elapsed.ToString("hh\\:mm\\:ss\\.fff"));

                    //j++;
                    i = 0;
                }

                i++;

                //Thread.Sleep(1);





                //CREACION DEL HILO PARA EJECUTAR LA LECTURA DE LOS EXPANDER


                /*
                 int tiempoVidaHilo = 60000; // 60 segundos

                Thread threadReaderExpander = new Thread(Reader_ExpanderAsync);
                threadReaderExpander.Name = "Expanders_Read";
                threadReaderExpander.Priority = ThreadPriority.Highest; //threar high priority
                threadReaderExpander.Start();
                Console.WriteLine("Hilo de lectura de expanders iniciado...");



                // Esperar el tiempo de vida del hilo secundario
                Thread.Sleep(tiempoVidaHilo);

                // Detener el hilo secundario de forma segura
                shouldStop = true;


                //Espera que finalize la ejecucion del hilo
                threadReaderExpander.Join();


                Console.WriteLine("Hilo Principal - Reiniciando el hilo de lectura de expanders...");

                // Esperar antes de crear un nuevo hilo secundario
                Thread.Sleep(100);

                // Reiniciar la variable shouldStop para permitir que el nuevo hilo funcione correctamente
                shouldStop = false;
                */

            }

        }


        public static async Task<Configuration> InitializeConfiguration()
        {
            Console.WriteLine("WELCOME TO MONITOR4 SYSTEM V2.0");
            Configuration? confg = null;

            while (confg == null)
            {
                Console.WriteLine("INSERT PATH ROUTE OF CONFIGURATION FILE: ");

                //string path = Console.ReadLine();
                string path = "/ConfigurationMonitor4";

                confg = new Configuration();
                confg = await confg.getConfigurationData(path);


                if (confg != null)
                {
                    i2cBus = int.Parse(confg.BoardConfig["I2cBus"]);
                    string cloudBroker = confg.BoardConfig["MqttCloudBroker"];
                    string portCloud = confg.BoardConfig["MqttCloudPort"];
                    string localBroker = confg.MqttBroker;
                    string portLocal = confg.MqttPort;
                    string clientName = confg.BoardConfig["MqttCloudClientName"];
                    string mqttTopic = confg.MqttTopic;
                    int RoomsQuantity = confg.RoomsQuantity;
                    rooms = new Room[RoomsQuantity];


                    Console.WriteLine($"MQTT LOCAL: {localBroker}:{portLocal}");
                    Console.WriteLine($"MQTT CLOUD: {cloudBroker}:{portCloud}");
                    Console.WriteLine("CLIENT NAME: " + clientName);
                    Console.WriteLine("MQTT TOPIC: " + mqttTopic);
                    Console.WriteLine("ROOMS QUANTITY: " + RoomsQuantity);
                    Console.WriteLine("ROOMS QUANTITY ASSIGNED: " + rooms.Length);
                    Console.WriteLine("I2C BUS: " + i2cBus);


                    Console.WriteLine("Is it Correct Configuration? Y/N");
                    //string res = Console.ReadLine();
                    string res = "Y";

                    if (res != "Y")
                    {
                        confg = null;
                    }
                }
                else
                {
                    continue;
                }


            }


            return confg;


        }


        //method to open channels i2c multiplexer
        public static void openMuxI2c(int i2cBus, int muxAdd, byte channel)
        {
            var settings = new I2cConnectionSettings(i2cBus, muxAdd);
            I2cDevice muxDevice = I2cDevice.Create(settings);
            muxDevice.WriteByte(channel);

        }

        //method to search i2c devices connected on bus
        public static void searchADD(int i2cBus, bool consoleActive)
        {
            int[] add = { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27 };
            int i = 0;
            foreach (int address in add)
            {

                var settings = new I2cConnectionSettings(i2cBus, address);
                I2cDevice device = I2cDevice.Create(settings);

                try
                {
                    device.WriteByte(0);
                    expanders[$"expander{i}"] = address; //store expanders and address in diccionary by reference
                    if (consoleActive) Console.WriteLine($"Dispositivo encontrado en la dirección: 0x{address:X}");
                    device.Dispose();
                }
                catch (Exception ex)
                {
                    if (consoleActive) Console.WriteLine($"No se encontro el dispositivo: 0x{address:X}");

                    continue;
                }
                i++;
            }
        }

        //method to config i2c expanders how outpus
        public static void configExpandersInputs()
        {

            byte[] bufferPort0 = new byte[] { PORT_SEL_REG_PORT0, 0xFF };
            byte[] bufferPort1 = new byte[] { PORT_SEL_REG_PORT1, 0xFF };

            foreach (var kvp in expanders)
            {

                try
                {

                    var settings = new I2cConnectionSettings(1, (int)kvp.Value);
                    I2cDevice expander = I2cDevice.Create(settings);

                    expander.Write(bufferPort0);
                    expander.Write(bufferPort1);

                    //clexpander.Dispose();
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"No se encontro el {kvp.Key}");
                    continue;
                }

            }




        }

        //method to read expanders
        private static void Reader_ExpanderAsync()
        {
            int DATA = 0;

            byte[] i2CReadBuffer = new byte[1] { 0x00 };

            //Stopwatch UpWatch = new();
            //UpWatch.Start();


            try
            {
                #region //LECTURA DE LOS EXPANDERS
                if (expander[0].getStatus() == true)
                {
                    //Console.WriteLine("expander0: " + expander[0].getAddress());

                    expander[0].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[0] = expander[0].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[0].BitArrayToString(SensorsGroups[0], 0); //convierte el bitarray en un string de 1 y 0

                    expander[0].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[1] = expander[0].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[0].BitArrayToString(SensorsGroups[1], 1); //convierte el bitarray en un string de 1 y 0

                }

                if (expander[1].getStatus() == true)
                {
                    //Console.WriteLine("expander1: " + expander[1].getAddress());

                    expander[1].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[2] = expander[1].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[1].BitArrayToString(SensorsGroups[2], 0); //convierte el bitarray en un string de 1 y 0

                    expander[1].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[3] = expander[1].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[1].BitArrayToString(SensorsGroups[3], 1); //convierte el bitarray en un string de 1 y 0
                }

                if (expander[2].getStatus() == true)
                {

                    //Console.WriteLine("expander2: " + expander[2].getAddress());

                    expander[2].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[4] = expander[2].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[2].BitArrayToString(SensorsGroups[4], 0); //convierte el bitarray en un string de 1 y 0

                    expander[2].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[5] = expander[2].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[2].BitArrayToString(SensorsGroups[5], 1); //convierte el bitarray en un string de 1 y 0

                }

                if (expander[3].getStatus() == true)
                {
                    //Console.WriteLine("expander3: " + expander[3].getAddress());

                    expander[3].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[6] = expander[3].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[3].BitArrayToString(SensorsGroups[6], 0); //convierte el bitarray en un string de 1 y 0

                    expander[3].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[7] = expander[3].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[3].BitArrayToString(SensorsGroups[7], 1); //convierte el bitarray en un string de 1 y 0

                }

                if (expander[4].getStatus() == true)
                {
                    //Console.WriteLine("expander4: " + expander[4].getAddress());

                    expander[4].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[8] = expander[4].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[4].BitArrayToString(SensorsGroups[8], 0); //convierte el bitarray en un string de 1 y 0

                    expander[4].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[9] = expander[4].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[4].BitArrayToString(SensorsGroups[9], 1); //convierte el bitarray en un string de 1 y 0

                }

                if (expander[5].getStatus() == true)
                {
                    //Console.WriteLine("expander5: " + expander[5].getAddress());

                    expander[5].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[10] = expander[5].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[5].BitArrayToString(SensorsGroups[10], 0); //convierte el bitarray en un string de 1 y 0

                    expander[5].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[11] = expander[5].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[5].BitArrayToString(SensorsGroups[11], 1); //convierte el bitarray en un string de 1 y 0
                }

                if (expander[6].getStatus() == true)
                {
                    //Console.WriteLine("expander6: " + expander[6].getAddress());

                    expander[6].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[12] = expander[6].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[6].BitArrayToString(SensorsGroups[12], 0); //convierte el bitarray en un string de 1 y 0

                    expander[6].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[13] = expander[6].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[6].BitArrayToString(SensorsGroups[13], 1); //convierte el bitarray en un string de 1 y 0
                }

                if (expander[7].getStatus() == true)
                {
                    //Console.WriteLine("expander7: " + expander[7].getAddress());

                    expander[7].WriteReadExpander(INPUT_REG_PORT0, i2CReadBuffer);// leer puerto 0
                    SensorsGroups[14] = expander[7].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[7].BitArrayToString(SensorsGroups[14], 0); //convierte el bitarray en un string de 1 y 0

                    expander[7].WriteReadExpander(INPUT_REG_PORT1, i2CReadBuffer);// leer puerto 1
                    SensorsGroups[15] = expander[7].ByteToBitArray(); // convierte el byte obtenido en un BitArray
                    expander[7].BitArrayToString(SensorsGroups[15], 1); //convierte el bitarray en un string de 1 y 0
                }
                #endregion
            }

            catch (Exception e)
            {
                Console.WriteLine("Expander Desconectado: " + e.Message);
            }



            DecodeRoomsAsync();

            //searchADD(i2cBus, false); //research i2c exapanders

            //configExpandersInputs(); //reconfig


        }

        //method to create i2c expanders
        private static void createExpanders(int bus)
        {
            int add = 0x20;
            for (int i = 0; i < expander.Length; i++)
            {
                expander[i] = new I2cExpander(i, bus, add);
                add++;
            }

            foreach (var kvp in expanders)
            {
                Console.WriteLine("Clave: " + kvp.Key + ", Valor: " + kvp.Value); //print expanders with add
            }


            Console.WriteLine("\nExpanders disponibles: " + expander.Length);


        }

        //method to create rooms
        private static void CreateRooms()
        {


            for (int i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new Room(i + 1); // Crear una nueva instancia de habitaciones y asignarla a la posición i de la matriz
                RoomDict.Add(i + 1, rooms[i]);
            }

            Console.WriteLine("\nHabitaciones disponibles: " + rooms.Length);
        }

        //method to decode data to rooms
        private static void DecodeRoomsAsync()
        {
            int value = 0;

            for (int x = 1; x <= rooms.Length; x++)
            {
                if (RoomDict.TryGetValue(x, out Room hab))
                {
                    //x - 1  = 0 / 4 = 0 
                    //x - 5 = 4 /4 = 1
                    int groupIndex = (x - 1) / 4;
                    int sensorIndex = ((x - 1) % 4) * 2;
                    hab.DecodeAsync(SensorsGroups[groupIndex].Get(sensorIndex), SensorsGroups[groupIndex].Get(sensorIndex + 1), mqttServiceCloud, mqttServiceLocal, mqttTopic);

                }
                else
                {
                    Console.WriteLine($"La habitación no está presente en el diccionario {x}");
                }
                /*
                for (int x = 0; x < rooms.Length; x = x + 4)//decodificacion de la comunicacion
                {





                        /*
                        int roomIndex0 = x;
                        int roomIndex1 = x + 1;
                        int roomIndex2 = x + 2;
                        int roomIndex3 = x + 3;

                        int sensorGroupIndex = x / 4 % 16;

                        if (SensorsGroups[sensorGroupIndex] != null)
                        {

                            rooms[roomIndex0].DecodeAsync(SensorsGroups[sensorGroupIndex].Get(0), SensorsGroups[sensorGroupIndex].Get(1), mqttServiceCloud, mqttServiceLocal, mqttTopic);

                             rooms[roomIndex1].DecodeAsync(SensorsGroups[sensorGroupIndex].Get(2), SensorsGroups[sensorGroupIndex].Get(3), mqttServiceCloud, mqttServiceLocal, mqttTopic);

                             rooms[roomIndex2].DecodeAsync(SensorsGroups[sensorGroupIndex].Get(4), SensorsGroups[sensorGroupIndex].Get(5), mqttServiceCloud, mqttServiceLocal, mqttTopic);

                             rooms[roomIndex3].DecodeAsync(SensorsGroups[sensorGroupIndex].Get(6), SensorsGroups[sensorGroupIndex].Get(7), mqttServiceCloud, mqttServiceLocal, mqttTopic);

                        }
                        value++;

                    }
            }   */


            }
        }
    }
}
