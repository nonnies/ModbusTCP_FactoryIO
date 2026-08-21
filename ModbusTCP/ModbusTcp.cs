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
        static TcpClient client;
        // Shared stop flag so the running background process can be stopped from the UI
        private static volatile bool stopRequested = false;
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

        public static void SimpleConveyor()
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
        

        public static void AssemblyBox()
        {
            if (client != null)
            {
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                
                ushort startAddress = 0;
                ushort numInputs = 15;
                // coil addresses for the assembly box process
                ushort MoveX = 0;
                ushort MoveZ = 1;
                ushort Grab = 2;
                ushort LidConveyor = 3;
                ushort ClampLid = 4;
                ushort PosRaiseLid = 5;
                ushort BaseConveyor = 6;
                ushort ClampBase = 7;
                ushort PosRaiseBase = 8;
                // input addresses for the assembly box process
                ushort LidAtPlace = 3;
                ushort BaseAtPlace = 6;


                

                // Clear any previous stop request and start the process
                stopRequested = false;
                Console.WriteLine("Starting the assembly box process...");
                bool LidPlaced = false;
                bool BasePlaced = false;

                while (true)
                {
                   
                    while (true)
                    {
                        bool[] inputs = master.ReadInputs(1, startAddress, numInputs);
                        Console.WriteLine("Running...");
                        
                        if (inputs[LidAtPlace] == true && inputs[BaseAtPlace] == true)
                        {

                            LidPlaced = true;
                            BasePlaced = true;
                            break;
                           
                        }
                        else if (inputs[BaseAtPlace] == true && inputs[LidAtPlace] == false)
                        {
                            BasePlaced = true;
                            LidPlaced = false;
                            break;
                        }
                        else if (inputs[BaseAtPlace] == false && inputs[LidAtPlace] == true)
                        {
                            BasePlaced = false;
                            LidPlaced = true;
                            break;
                        }
                        else
                        {
                            LidPlaced = false;
                            BasePlaced = false;
                            break;
                        }
                    
                    }
                    Console.WriteLine($"LidAtPlace: {LidPlaced}, BaseAtPlace: {BasePlaced}]");

                    if (LidPlaced == true && BasePlaced == true)
                    {
                        Thread.Sleep(50);
                        master.WriteSingleCoil(1, PosRaiseBase, false);
                        Thread.Sleep(950);
                        master.WriteSingleCoil(1, LidConveyor, false);
                        master.WriteSingleCoil(1, BaseConveyor, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, ClampBase, true);
                        master.WriteSingleCoil(1, ClampLid, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveZ, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, Grab, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, ClampLid, false);
                        master.WriteSingleCoil(1, ClampBase, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveZ, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveX, true);

                        const int pollIntervalMs = 50; // wait between polls
                        const int timeoutMs = 10000; // maximum wait (10 seconds)
                        int elapsed = 0;

                        while (elapsed < timeoutMs)
                        {
                            bool[] MovingX = master.ReadInputs(1, 0, 1);
                            Console.WriteLine("Moving X...");

                            if (MovingX[0] == false)
                            {
                                Console.WriteLine("Finished moving X.");
                                break;
                            }

                            Thread.Sleep(pollIntervalMs);
                            elapsed += pollIntervalMs;
                        }

                        Thread.Sleep(1000);
                        master.WriteSingleCoil(1, MoveZ, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, Grab, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveZ, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveX, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, PosRaiseBase, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, BaseConveyor, true);
                        master.WriteSingleCoil(1, LidConveyor, true);
                        Thread.Sleep(600);

                    }
                    else if (LidPlaced == true && BasePlaced == false)
                    {
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, LidConveyor, false);
                        master.WriteSingleCoil(1, BaseConveyor, true);
                        
                    }
                    else if (LidPlaced == false && BasePlaced == true)
                    {
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, BaseConveyor, false);
                        master.WriteSingleCoil(1, LidConveyor, true);

                    }
                    else
                    {
                        
                        master.WriteSingleCoil(1, BaseConveyor, true);
                        master.WriteSingleCoil(1, LidConveyor, true);
                        
                    }

                    

                    while (true)
                    {
                        bool[] partLeaving = master.ReadInputs(1, 9, 1);
                        if (partLeaving[0] == true || BasePlaced == false || LidPlaced == false)
                        {
                            master.WriteSingleCoil(1, PosRaiseBase, false);
                            break;
                        }

                        Thread.Sleep(50);
                    }
                    Thread.Sleep(100);




                }
            }
        }
        
    }
}
