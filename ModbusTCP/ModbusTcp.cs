using NModbus;
using System;
using System.Collections.Generic;
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
        static TcpClient client;
        public static void ModbusTcpConnection(string action)
        {
            if (action == "START")
            {
                client = new TcpClient("192.168.50.52", 502);
                Console.WriteLine("TCP client initialized.");
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
            }
        }

        public static bool[] ModbusTcpMasterReadInputs(ushort registerAdr)
        {
            if (client != null)
            {
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);

                // read five input values
                ushort startAddress = (ushort)(registerAdr - 1);
                ushort numInputs = 5;
                bool[] inputs = master.ReadInputs(1, startAddress, numInputs);

                for (int i = 0; i < numInputs; i++)
                {
                    Console.WriteLine($"Input {(registerAdr + i)}={(inputs[i] ? 1 : 0)}");
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
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                // write a single coil
                ushort startAddress = (ushort)(registerAdr - 1);
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
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                // write a single holding register
                ushort startAddress = (ushort)(registerAdr - 1);
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
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                ushort numRegisters = 5;
                ushort startAddress = (ushort)(registerAdr - 1);
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

        public static void StopOnSensor()
        {
            if (client != null)
            {
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                ushort startAddress = 0;
                Console.WriteLine("Start the program, conveyor starting...");
                master.WriteSingleCoil(1, startAddress, true);

                while (true)
                {
                    bool[] inputs = master.ReadInputs(1, startAddress, 2);
                    Console.WriteLine("Checking...");
                    if (inputs[0] == false && inputs[1] == true)
                    {
                        Console.WriteLine("Sensor is active. Stopping the process.");
                        // Implement the logic to stop the process here
                        master.WriteSingleCoil(1, startAddress, false);
                        Console.WriteLine($"Conveyor stopped.");
                        break;
                    }

                    Thread.Sleep(50);
                }
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
            }
        }

        public static void Reset()
        {
            if (client != null)
            {
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                int max_coils = 12;
                ushort[] startAddress = Enumerable.Range(0, max_coils).Select(i => (ushort)i).ToArray();

                Console.WriteLine("Resetting the process...");
                foreach (ushort address in startAddress)
                {
                    master.WriteSingleCoil(1, address, false);
                }
                Console.WriteLine($"Process reset.");
            }
            else
            {
                Console.WriteLine("TCP client is not initialized.");
            }
        }

        public static void AssemblyBox()
        {
            if (client != null)
            {
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                ushort startAddress = 0;
                ushort numInputs = 15;
                ushort LidAtPlace = 3;
                ushort MoveX = 0;
                ushort MoveZ = 1;
                ushort Grab = 2;
                ushort LidConveyor = 3;
                ushort ClampLid = 4;
                Console.WriteLine("Starting the assembly box process...");
                master.WriteSingleCoil(1, LidConveyor, true);

                while (true)
                {
                    bool[] inputs = master.ReadInputs(1, startAddress, numInputs);
                    Console.WriteLine("Running...");
                    if (inputs[LidAtPlace] == true)
                    {
                        Thread.Sleep(1000);
                        master.WriteSingleCoil(1, LidConveyor, false);

                        break;
                    }
                }
                Thread.Sleep(500);
                master.WriteSingleCoil(1, ClampLid, true);
                Thread.Sleep(500);
                master.WriteSingleCoil(1, MoveZ, true);
                Thread.Sleep(500);
                master.WriteSingleCoil(1, Grab, true);
                Thread.Sleep(500);
                master.WriteSingleCoil(1, MoveZ, false);
                Thread.Sleep(500);
                master.WriteSingleCoil(1, MoveX, true);
                Thread.Sleep(500);
                while (true)
                {
                    bool[] MovingX = master.ReadInputs(1, 0, 1);
                    Console.WriteLine("Moving X...");
                    if (MovingX[0] == false)
                    {
                        Console.WriteLine("Finished moving X.");
                        break;
                    } 
                }
                Thread.Sleep(500);
                master.WriteSingleCoil(1, MoveZ, true);
            }
        }
    }
}
