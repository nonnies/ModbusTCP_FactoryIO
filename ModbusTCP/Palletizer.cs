using NModbus;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModbusTCP
{
    internal class Palletizer : ModbusTcp
    {
        public bool readyToDrop = false;

        public async Task BoxFeederAsync(CancellationToken token)
        {
            if (client == null)
            {
                Console.WriteLine("TCP client is not initialized.");
                return;
            }
            //coil address
            ushort boxFeeder = 0;
            ushort turnBox = 1;
            ushort push = 2;
            ushort loadBelt = 3;
            ushort clamp = 4;
            //input address
            ushort boxAtEntry = 0;
            //var
            ushort countBox = 0;
            ushort countPush = 0;
            int boxMovingPollRate = 600;
            

            try {
                while (!token.IsCancellationRequested)
                {
                    Console.WriteLine("Running");
                    master.WriteSingleCoil(1, boxFeeder, true);
                    master.WriteSingleCoil(1, loadBelt, true);
                    master.WriteSingleCoil(1, turnBox, true);
                    //await Task.Delay(500);
                    
                    bool[] input = master.ReadInputs(1, boxAtEntry, 1);
                    bool boxReady = input[0];
                    

                    //Console.WriteLine(boxReady);
                 
                    if (!boxReady)
                    {
                        countBox++;
                        
                    }
                   Console.WriteLine(countBox);
                    if (countBox == 3)
                    {
                        await Task.Delay(500);
                        master.WriteSingleCoil(1, boxFeeder, false);
                        await Task.Delay(1400);
                        //master.WriteSingleCoil(1, loadBelt, false);
                        master.WriteSingleCoil(1, push, true);
                        countPush++;
                        countBox = 0;
                        await Task.Delay(1500);
                    }
                    else
                    {
                        master.WriteSingleCoil(1, push, false);
                    }

                    if (countPush == 2)
                    {
                        master.WriteSingleCoil(1, clamp, true);
                        readyToDrop = true;
                        await Task.Delay(1500);
                        master.WriteSingleCoil(1, clamp, false);
                        Console.WriteLine("ready to drop!");
                        countPush = 0;
                    }
                    else
                    {
                        readyToDrop = false;
                    }

                    await Task.Delay(boxMovingPollRate);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task PalletFeederAsync(CancellationToken token)
        {
            if (client == null)
            {
                Console.WriteLine("TCP client is not initialized.");
                return;
            }
            //coil address
            ushort palletFeeder = 5;
            ushort loadPallet = 6;
            ushort elevatorUp = 7;
            ushort elevatorDown = 8;
            ushort openPlate = 9;
            ushort exitConveyor = 10;
            //input address
            ushort palletAtEn = 1;
            ushort palletLoaded = 2;
            ushort palletAtEx = 3;

            while (!token.IsCancellationRequested)
            {
                
                bool[] input = master.ReadInputs(1, palletAtEn, 1);
                bool palletEnterd = false;
                bool palletLoadedNow = false;

                if (!input[0])
                {
                    palletEnterd = true;
                }
                
                
                if (palletEnterd) 
                {
                    Console.WriteLine("palletEnterd");
                    await Task.Delay(2000);
                    master.WriteSingleCoil(1, palletFeeder, false);
                    master.WriteSingleCoil(1, loadPallet, true);
                }
                else
                {
                    master.WriteSingleCoil(1, palletFeeder, true);
                }

                while (palletEnterd)
                {
                    bool[] input2 = master.ReadInputs(1, palletLoaded, 1);
                    

                    if (input2[0])
                    {
                        palletLoadedNow = true;
                    }

                    if (palletLoadedNow)
                    {
                        Console.WriteLine("pallet loaded");
                        master.WriteSingleCoil(1, loadPallet, false);
                        master.WriteSingleCoil(1, palletFeeder, false);
                        break;
                    }
                    await Task.Delay(50);
                }
                if (palletLoadedNow)
                {
                    await Task.Delay(500);
                    master.WriteSingleCoil(1, elevatorUp, true);
                    await Task.Delay(2000);
                    master.WriteSingleCoil(1, elevatorUp, false);
                    while (true)
                    {
                        if (readyToDrop)
                        {
                            master.WriteSingleCoil(1, openPlate, true);
                            await Task.Delay(2000);
                            master.WriteSingleCoil(1, elevatorDown, true);
                            await Task.Delay(2000);
                            master.WriteSingleCoil(1, openPlate, false);
                            
                            readyToDrop = false;
                            break;
                        }
                        await Task.Delay(100);
                    }
                    master.WriteSingleCoil(1, loadPallet, true);
                    master.WriteSingleCoil(1, exitConveyor, true);
                    await Task.Delay(3000);
                }
                else
                {
                    master.WriteSingleCoil(1, elevatorUp, false);
                    master.WriteSingleCoil(1, elevatorDown, false);
                }

                await Task.Delay(50);
            }

        }

        public void StopProgram()
        {
            Reset();
        }
    }
}
