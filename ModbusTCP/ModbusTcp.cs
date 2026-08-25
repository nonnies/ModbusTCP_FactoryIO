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
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
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
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
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
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
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
    class Conveyor : ModbusTcp
    {
        // Shared stop flag so the running background process can be stopped from the UI
        private static volatile bool stopRequested = false;
        // Event raised when the continuous count changes
        public static event Action<ushort> CountUpdated;
        public static void SimpleConveyor()
        {
            if (client != null)
            {
                var factory = new ModbusFactory();
                IModbusMaster master = factory.CreateMaster(client);
                ushort startAddress = 0;
                Console.WriteLine("Start the program, conveyor starting...");
                master.WriteSingleCoil(1, startAddress, true);

                while (!stopRequested)
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
        public static void RequestStop()
        {
            stopRequested = true;
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
        public static void BoxAssembler()
        {
            if (client == null)
            {
                Console.WriteLine("TCP client is not initialized.");
                return;
            }

            try
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
                ushort BaseConveyor = 6;
                ushort ClampBase = 7;
                ushort PosRaiseBase = 8;
                // input addresses for the assembly box process (zero-based index into the inputs[] array)
                ushort LidAtPlace = 3;
                ushort BaseAtPlace = 6;
                ushort PartLeaving = 9;

                bool status = false;
                ushort counter = 0;

                Console.WriteLine("Starting the assembly box process...");
                bool LidPlaced = false;
                bool BasePlaced = false;

                while (!stopRequested)
                {
                    status = false;

                    // Poll inputs once per cycle (breaks immediately to decide flow)
                    bool[] inputs = master.ReadInputs(1, startAddress, numInputs);
                    Console.WriteLine("Running...");

                    if (stopRequested) break;

                    if (inputs[LidAtPlace] && inputs[BaseAtPlace])
                    {
                        LidPlaced = true; BasePlaced = true;
                    }
                    else if (inputs[BaseAtPlace] && !inputs[LidAtPlace])
                    {
                        BasePlaced = true; LidPlaced = false;
                    }
                    else if (!inputs[BaseAtPlace] && inputs[LidAtPlace])
                    {
                        BasePlaced = false; LidPlaced = true;
                    }
                    else
                    {
                        LidPlaced = false; BasePlaced = false;
                    }

                    Console.WriteLine($"LidAtPlace: {LidPlaced}, BaseAtPlace: {BasePlaced}]");

                    if (stopRequested) break;

                    if (LidPlaced && BasePlaced)
                    {

                        status = true;
                        Thread.Sleep(500);
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
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveZ, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveX, true);

                        const int pollIntervalMs = 50; // wait between polls
                        const int timeoutMs = 10000; // maximum wait (10 seconds)
                        int elapsed = 0;

                        while (elapsed < timeoutMs && !stopRequested)
                        {
                            bool[] MovingX = master.ReadInputs(1, 0, 1);
                            Console.WriteLine("Moving X...");

                            if (stopRequested) break;

                            if (MovingX[0] == false)
                            {
                                Console.WriteLine("Finished moving X.");
                                break;
                            }

                            Thread.Sleep(pollIntervalMs);
                            elapsed += pollIntervalMs;
                        }

                        if (stopRequested) break;

                        Thread.Sleep(1000);
                        master.WriteSingleCoil(1, MoveZ, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, Grab, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, MoveZ, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, ClampBase, false);
                        master.WriteSingleCoil(1, MoveX, false);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, PosRaiseBase, true);
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, BaseConveyor, true);
                        master.WriteSingleCoil(1, LidConveyor, true);
                    }
                    else if (LidPlaced && !BasePlaced)
                    {
                        Thread.Sleep(500);
                        master.WriteSingleCoil(1, LidConveyor, false);
                        master.WriteSingleCoil(1, BaseConveyor, true);
                    }
                    else if (!LidPlaced && BasePlaced)
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

                    while (status && !stopRequested)
                    {
                        bool[] Inputs = master.ReadInputs(1, startAddress, numInputs);
                        if (Inputs[PartLeaving] == true)
                        {
                            Thread.Sleep(100);
                            master.WriteSingleCoil(1, PosRaiseBase, false);
                            counter++;
                            Console.WriteLine($"Part counted -> {counter}");
                            // notify UI about the new counter value
                            CountUpdated?.Invoke(counter);
                            break;
                        }

                        Thread.Sleep(50); // throttle poll
                    }

                    Thread.Sleep(100);
                    master.WriteSingleRegister(1, 0, counter);
                }

                Console.WriteLine("AssemblyBox: exiting.");
                Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssemblyBox exception: {ex}");
            }
            finally
            {
                // Ensure stop flag cleared when method completes
                stopRequested = false;
            }
        }
    }
}
