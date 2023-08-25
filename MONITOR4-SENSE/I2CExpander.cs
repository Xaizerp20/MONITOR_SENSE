using System;
using System.Collections;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONITOR4
{
    public class I2cExpander
    {
        private int id { set; get; }
        private int bus { set; get; }
        private int address { set; get; }
        private bool status { set;  get; }
        I2cDevice expander { set; get; }
        byte[] buffer { set; get; }


        //constructor del expander
        public I2cExpander(int id, int bus, int address)
        {
            this.id = id;
            this.bus = bus;
            this.address = address;

            var settings = new I2cConnectionSettings(this.bus, this.address);
            expander = I2cDevice.Create(settings);

            const byte PORT_SEL_REG_PORT0 = 0X06; //register to config port 0 how I/O
            const byte PORT_SEL_REG_PORT1 = 0X07; //register to config port 1 how I/O

            byte[] bufferPort0 = new byte[] { PORT_SEL_REG_PORT0, 0xFF };
            byte[] bufferPort1 = new byte[] { PORT_SEL_REG_PORT1, 0xFF };

            try
            {

                //expander.WriteByte(0);
                expander.Write(bufferPort0);
                expander.Write(bufferPort1);
                //expander.Dispose();
                this.status = true; //si todo fue correcto el expaner estara disponible
            }
            catch (IOException)
            {
                Console.WriteLine($"Expander {this.id} Not found");
                this.status = false; //coloca el expander como desactivado
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                this.status = false; //coloca el expander como desactivado
            }

        }

        //metodo para escribir un registro del expander y luego leer dicho registro
        public void WriteReadExpander(byte reg, byte[] buffer)
        {
            try
            {
                expander.WriteRead(new byte[] { reg }, buffer);

                this.buffer = buffer;
            }
            catch (Exception ex)
            {
                this.status = false;
                Console.WriteLine("Error de Lectura");
            }
       
        }

        //metodo para convertir el byte leido en un bitarray
        public BitArray ByteToBitArray()
        {

            BitArray arraybit = new BitArray(new[] { buffer[0] });// convierte el byte en BitArray

            return arraybit;
        }

        public string BitArrayToString(BitArray SensorGroup, int port)
        {
            string bits = string.Concat(SensorGroup.Cast<bool>().Select(bit => bit ? "1" : "0"));
            //Console.Write($"\t\tExpander {this.id} port {port}: {bits}\n");

            return bits;
        }
   
        public int getId()
        {
            return this.id;
        }

        public int getAddress()
        {
            return this.address;
        }
    
        public bool getStatus()
        {
            return this.status;
        }
    }

  
    
}
