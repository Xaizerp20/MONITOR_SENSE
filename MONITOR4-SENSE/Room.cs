using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONITOR4
{
    public class Room
    {
        private int Cont_err = 0;
        private int Capt = 0;
        public int Cont_data { get; set; }  = 0;
        public int Trama_V { get; set; } = 0;
        public int NumHab { get; set; } // numero de habitacion 
        public static MQTT mqttService = new MQTT();
        public static Stopwatch cont_err_timer = new Stopwatch();


        //constructor de habitacion y necesita almenos el numero de la habitacion
        public Room(int NumHab) 
        {
            this.NumHab = NumHab;
        }

        //metodo para decodificar la data recibida
        public int DecodeAsync(bool D0, bool D1, MQTT _mqttServiceCloud, MQTT _mqttServiceLocal, string mqttTopic)
        {
           
            //estados que llegan desde la tarjeta en la habitacion
            int ESTADO_ERROR = 0;
            int ESTADO_INIT = 1;
            int ESTADO_LIMPIEZA = 2;
            int ESTADO_OCUPADA = 3;
            int ESTADO_LIBRE = 4;
            int ESTADO_MANTENIMIENTO = 5;
            int ESTADO_CERRADA = 6;
            int ESTADO_ABIERTA = 7;
            int TipoTarjetaLimpieza = 1;
            int TipoTarjetaMantenimineto = 2;
            int TipoTarjetaCheking = 3;
            //verifica si tiene mucho tiempo sin mandar data (tiempo en segundos)
            /*
            if (ContTimeTiempoAveria > 60)
            {
                ESTADO_HAB = ESTADOS.AVERIA;
            }
            */
            cont_err_timer.Start();
            if (D0 == true && D1 == true)//si estan los bits en reposo
            {

                Cont_err++;//aumenta el contador para saber cuando pasa mucho tiempo en pausa
                Capt = 0;//indica que este bit no es valido espera el siguiente

                //verifica que el tiempo en reposo indica una nueva trama
                /*
                if (Cont_err > 10)
                {
                    Cont_data = 0;//comienza desde cero la captura de bits
                    Trama_V = 0;
                }
                */

                if (cont_err_timer.ElapsedMilliseconds > 250)
                {
                    Cont_data = 0;
                    Trama_V = 0;
                }

            }
            else //si no significa que capturamos algo
            {

                if (Capt == 0)
                {

                    if (D0 == false && D1 == true)
                    {
                        Trama_V &= ~(1 << (Cont_data)); 
                        Cont_data++;
                        Capt = 1;
                        Cont_err = 0;
                        cont_err_timer.Reset(); 
                        Console.WriteLine("Cont data: " + Cont_data);
                    }
                    else if (D0 == true && D1 == false)
                    {
                        Trama_V |= (1 << (Cont_data));
                        Cont_data++;
                        Capt = 1;
                        Cont_err = 0;
                        cont_err_timer.Reset();
                        Console.WriteLine("Cont data: " + Cont_data);
                    }

                    //trama completa captura de 20bits con codificacion 2023
                    if (Cont_data == 20)
                    {

                        int DATA = Trama_V;
                        Console.WriteLine("DATA: " + DATA);
                        /*
                        //decodificacion trama version 2023
                        int ID_TARJETA = (DATA & 0xFF);//ID de la tarjeta
                        int TIPO_TARJETA = ((DATA >> 8) & 0x0F);//tipo de tarjeta
                        int ESTADO_HABITACION = ((DATA >> 12) & 0x0F);//estado de la habitacion
                        int OPC = ((DATA >> 16) & 0x0F);//verifica si la trama es de tipo OPC
                        
                        Console.WriteLine("DATA: " + DATA);
                        Console.WriteLine("ID TARJETA: " + ID_TARJETA);
                        Console.WriteLine("TIPO TARJETA: " + TIPO_TARJETA);
                        Console.WriteLine("ESTADO HABITACION: " + ESTADO_HABITACION);
                        Console.WriteLine("OPC: " + OPC);
                        */

                        string json = JsonConvert.SerializeObject(this);
                        Console.Write(json);

                        string reformatTopic = mqttTopic.Replace("/#", "/Room");
                        string topic = $"{reformatTopic}/{this.NumHab}";
                        

                        Task.Run(async () => { await _mqttServiceLocal.sendMessage($"{json}", topic); });
                        Task.Run(async () => { await _mqttServiceCloud.sendMessage($"{json}", topic); });

                        return DATA;
                        
                    }
                }
            }

            return 0;
        }


    }
}
