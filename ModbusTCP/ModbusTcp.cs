using NModbus;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Text;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace ModbusTCP
{
    internal class ModbusTcp
    {
        public static TcpClient client;
        public static IModbusMaster master;
        public static void ModbusTcpConnection(string action)
        {
            if (action == "START")
            {
                client = new TcpClient("192.168.50.52", 502);
                var factory = new ModbusFactory();
                master = factory.CreateMaster(client);
                Console.WriteLine("TCP client & master initialized.");
            }
            else if (action == "STOP")
            {
                if (client != null)
                {
                    client.Dispose();
                    client.Close();
                    client = null;
                    Console.WriteLine("TCP client closed.");
                }
                master = null;
            }
        }
        public static void Reset()
        {
            if (client != null)
            {
                ushort startAddress = 0;
                Console.WriteLine("Resetting the process...");
                master.WriteMultipleCoils(1, startAddress, Enumerable.Repeat(false, 12).ToArray()); // Assuming 12 coils to stop
                Console.WriteLine($"Process reset.");
               
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
            }
        }

        public static bool[] ModbusTcpMasterReadInputs(ushort registerAdr)
        {
            if (client != null)
            {
                // read five input values
                ushort startAddress = registerAdr;
                ushort numInputs = 5;
                bool[] inputs = master.ReadInputs(1, startAddress, numInputs);

                for (int i = 0; i < numInputs; i++)
                {
                    Console.WriteLine($"Input {(registerAdr + i)} = {inputs[i]}");
                }
                return inputs;
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
                // return an empty array to satisfy the bool[] return type on all code paths
                return Array.Empty<bool>();
            }
            // output: 
            // Input 100=0
            // Input 101=0
            // Input 102=0
            // Input 103=0
            // Input 104=0
        }

        public static void ModbusTcpMasterWriteSingleCoil(ushort registerAdr, bool value)
        {
            if (client != null)
            {
                // write a single coil
                ushort startAddress = registerAdr;
                master.WriteSingleCoil(1, startAddress, value);
                Console.WriteLine($"Wrote value {value} to coil {registerAdr}");
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
            }
        }



        public static void ModbusTcpMasterWriteSingleRegister(ushort registerAdr, ushort value)
        {
            if (client != null)
            {
                // write a single holding register
                ushort startAddress = registerAdr;
                master.WriteSingleRegister(1, startAddress, value);
                Console.WriteLine($"Wrote value {value} to register {registerAdr}");
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
            }
        }

        public static ushort[] ModbusTcpMasterReadHoldingRegisters(ushort registerAdr)
        {
            if (client != null)
            {
                ushort numRegisters = 5;
                ushort startAddress = registerAdr;
                ushort[] values = master.ReadHoldingRegisters(1, startAddress, numRegisters);
                for (int i = 0; i < numRegisters; i++)
                {
                    Console.WriteLine($"Holding Register {(registerAdr + i)}={values[i]}");
                }
                return values; // Return all values read
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
                return Array.Empty<ushort>();
            }
        }
        
    }
    
}
