using NModbus;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        public bool craneReady = true;
        public bool craneLoadEmpty = true;
        private readonly BlockingCollection<DateTime> _boxEvents = new BlockingCollection<DateTime>();

        private async Task PollBoxSensorAsync(ushort boxAtEntry, CancellationToken token)
        {
            // initial previous state
            bool previous = true;
            try
            {
                bool[] prevArr = await master.ReadInputsAsync(1, boxAtEntry, 1).ConfigureAwait(false);
                previous = prevArr[0];
            }
            catch { /* ignore initial read errors */ }

            DateTime lastDetected = DateTime.MinValue;
            const int pollMs = 5;
            const int debounceMs = 50;

            while (!token.IsCancellationRequested)
            {
                    try
                    {
                        bool[] currArr = await master.ReadInputsAsync(1, boxAtEntry, 1).ConfigureAwait(false);
                        bool curr = currArr[0];
                    if (previous && !curr)
                    {
                        var now = DateTime.UtcNow;
                        if ((now - lastDetected).TotalMilliseconds > debounceMs)
                        {
                            _boxEvents.Add(now);
                            lastDetected = now;
                        }
                    }
                    previous = curr;
                }
                catch
                {
                    await Task.Delay(200, token).ConfigureAwait(false);
                }

                await Task.Delay(pollMs, token).ConfigureAwait(false);
            }
        }
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
            int boxMovingPollRate = 10;

            try
            {
                Console.WriteLine("Start Running");
                // start sensor poller that enqueues falling edges on the thread-pool
                var pollTask = Task.Run(async () => await PollBoxSensorAsync(boxAtEntry, token));

                while (!token.IsCancellationRequested)
                {
                    // ensure actuators are enabled each cycle
                    await master.WriteSingleCoilAsync(1, boxFeeder, true);
                    await master.WriteSingleCoilAsync(1, loadBelt, true);
                    await master.WriteSingleCoilAsync(1, turnBox, true);

                    // consume any sensor events produced by the poller
                    DateTime ev;
                    while (_boxEvents.TryTake(out ev))
                    {
                        countBox++;
                        Console.WriteLine($"Box detected at {ev:O} -> {countBox}");
                    }
                   
                    if (countBox == 3)
                    {
                        await Task.Delay(500);
                        await master.WriteSingleCoilAsync(1, boxFeeder, false);
                        await Task.Delay(1400);
                        //master.WriteSingleCoil(1, loadBelt, false);
                        await master.WriteSingleCoilAsync(1, push, true);
                        countPush++;
                        countBox = 0;
                        await Task.Delay(1500);
                    }
                    else
                    {
                        await master.WriteSingleCoilAsync(1, push, false);
                    }

                    if (countPush == 2)
                    {
                        await master.WriteSingleCoilAsync(1, clamp, true);
                        await Task.Delay(500);
                        Console.WriteLine("ready to drop!");
                        readyToDrop = true;
                        await Task.Delay(3000);
                        await master.WriteSingleCoilAsync(1, clamp, false);
                        
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
            

            while (!token.IsCancellationRequested)
            {
                
                bool[] input = await master.ReadInputsAsync(1, palletAtEn, 1);
                bool palletEnterd = false;
                bool palletLoadedNow = false;

                if (!input[0])
                {
                    palletEnterd = true;
                }
                
                
                if (palletEnterd) 
                {
                    if (!palletLoadedNow)
                    {
                        await master.WriteSingleCoilAsync(1, palletFeeder, true);
                    }
                    Console.WriteLine("palletEnterd");
                    await Task.Delay(2000);
                    await master.WriteSingleCoilAsync(1, palletFeeder, false);
                    await master.WriteSingleCoilAsync(1, loadPallet, true);
                }
                else
                {
                    await master.WriteSingleCoilAsync(1, palletFeeder, true);
                }

                while (palletEnterd && !token.IsCancellationRequested)
                {
                    bool[] input2 = await master.ReadInputsAsync(1, palletLoaded, 1);

                    if (input2[0])
                    {
                        palletLoadedNow = true;
                    }

                    if (palletLoadedNow)
                    {
                        Console.WriteLine("pallet loaded");
                        await master.WriteSingleCoilAsync(1, loadPallet, false);
                        await master.WriteSingleCoilAsync(1, palletFeeder, false);
                        break;
                    }
                    await Task.Delay(10);
                }
               
                if (palletLoadedNow)
                {
                    Console.WriteLine("start elevator");
                    await Task.Delay(500);
                    await master.WriteSingleCoilAsync(1, elevatorUp, true);
                    await Task.Delay(2000);
                    await master.WriteSingleCoilAsync(1, elevatorUp, false);
                    while (!token.IsCancellationRequested)
                    {
                        if (readyToDrop)
                        {
                            await master.WriteSingleCoilAsync(1, openPlate, true);
                            await Task.Delay(3000);
                            await master.WriteSingleCoilAsync(1, elevatorDown, true);
                            await Task.Delay(2000);
                            await master.WriteSingleCoilAsync(1, openPlate, false);
                            await master.WriteSingleCoilAsync(1, elevatorDown, false);

                            break;
                        }
                        await Task.Delay(100);
                    }
                    await master.WriteSingleCoilAsync(1, loadPallet, true);
                    await master.WriteSingleCoilAsync(1, exitConveyor, true);
                    await Task.Delay(3000);
                }
                else
                {
                    
                    await master.WriteSingleCoilAsync(1, elevatorUp, false);
                    await master.WriteSingleCoilAsync(1, elevatorDown, false);
                }

                await Task.Delay(40);
            }

        }

        public async Task TurnableAsync(CancellationToken token)
        {
            if (client == null)
            {
                Console.WriteLine("TCP client is not initialized.");
                return;
            }

            ushort exitConveyor = 10;
            ushort turnLoad = 11;
            ushort turn = 13;

            ushort craneEntry = 14;
            ushort craneLoad = 15;

            ushort entryTurn = 5;
            ushort atFront = 6;
            bool prevEntryTurn = true;
            

            while (!token.IsCancellationRequested)
            {
                bool[] input = await master.ReadInputsAsync(1, entryTurn, 1);
                bool enteredTurn = !input[0] && prevEntryTurn;
                prevEntryTurn = input[0];
                if (enteredTurn)
                {
                    await master.WriteSingleCoilAsync(1, turnLoad, true);
                    await Task.Delay(2000);
                    await master.WriteSingleCoilAsync(1, exitConveyor, false);
                }
                else
                {
                    await master.WriteSingleCoilAsync(1, turnLoad, false);
                    await master.WriteSingleCoilAsync(1, exitConveyor, true);
                }

                while (enteredTurn && !token.IsCancellationRequested)
                {
                    bool[] input2 = await master.ReadInputsAsync(1, atFront, 1);
                    bool fronted = input2[0];
                    if (fronted)
                    {
                        await master.WriteSingleCoilAsync(1, turnLoad, false);
                        break;
                    }
                }

                bool[] directions = await master.ReadInputsAsync(1, 8, 2);
                bool toPallet = directions[0];
                bool toCrane = directions[1];
                if (enteredTurn && toPallet)
                {
                    await master.WriteSingleCoilAsync(1, turn, true);
                    await Task.Delay(2000);
                    await master.WriteSingleCoilAsync(1, turnLoad, true);
                    await master.WriteSingleCoilAsync(1, craneEntry, true);
                    await Task.Delay(3000);
                    await master.WriteSingleCoilAsync(1, craneEntry, false);

                }
                else if (enteredTurn && toCrane)
                {
                    await master.WriteSingleCoilAsync(1, turn, false);
                    await Task.Delay(2000);
                    await master.WriteSingleCoilAsync(1, turnLoad, true);
                    await master.WriteSingleCoilAsync(1, craneEntry, true);
                    await Task.Delay(3000);
                    await master.WriteSingleCoilAsync(1, craneEntry, false);


                }
                else
                {
                    await master.WriteSingleCoilAsync(1, turn, false);
                }

                bool entryCrane = (await master.ReadInputsAsync(1, 10, 1))[0];

                if (!entryCrane)
                {
                    craneLoadEmpty = false;
                }

                if (craneLoadEmpty && !craneReady)
                {
                    await master.WriteSingleCoilAsync(1, craneEntry, true);
                    await master.WriteSingleCoilAsync(1, craneLoad, true);
                }
                else if (!craneLoadEmpty && !craneReady)
                {
                    await master.WriteSingleCoilAsync(1, craneLoad, false);
                    await master.WriteSingleCoilAsync(1, craneEntry, false);
                }
                else if (!craneLoadEmpty && craneReady)
                {
                    await master.WriteSingleCoilAsync(1, craneLoad, false);
                    await master.WriteSingleCoilAsync(1, craneEntry, true);
                }
                else
                {
                    await master.WriteSingleCoilAsync(1, craneEntry, true);
                    await master.WriteSingleCoilAsync(1, craneLoad, true);
                }



                await Task.Delay(10);
            }
        }

        public async Task CraneAsync(CancellationToken token)
        {
            //coils address
            ushort craneLeft = 16;
            ushort craneRight = 17;
            ushort craneLift = 18;

            ushort cranePos = 0;

            if (client == null)
            {
                Console.WriteLine("TCP client is not initialized.");
                return;
            }
            ushort pos = 9;
            while (!token.IsCancellationRequested)
            {
                bool entryCrane = (await master.ReadInputsAsync(1, 10, 1))[0];
                bool lifted = false;

                if (!entryCrane && craneReady)
                {
                    await master.WriteSingleCoilAsync(1, craneLeft, true);
                    await Task.Delay(1500);
                    await master.WriteSingleCoilAsync(1, craneLift, true);
                    await Task.Delay(1500);
                    await master.WriteSingleCoilAsync(1, craneLeft, false);
                    craneLoadEmpty = true;
                    await Task.Delay(1000);
                    lifted = true;
                }
                else
                {
                    await master.WriteSingleCoilAsync(1, craneLeft, false);
                }

                if (craneReady && lifted)
                {
                    // Wait for final motion to start and stop (debounced)
                    bool[] lastRead = null;
                    int stableCount = 0;
                    bool motionSeen = false;
                    const int requiredStable = 3;
                    const int motionStartTimeoutMs = 3000;

                    DateTime cmdTime = DateTime.UtcNow;

                    craneReady = false;
                    lifted = false;

                    await master.WriteSingleRegisterAsync(1, cranePos, pos);
                    while (!token.IsCancellationRequested)
                    {
                        bool[] movingXZ = await master.ReadInputsAsync(1, 11, 2);
                        bool isMoving = movingXZ[0] || movingXZ[1];

                        if (!motionSeen)
                        {
                            if (isMoving)
                            {
                                Console.WriteLine("motionSeen");
                                motionSeen = true;
                                stableCount = 0;
                                lastRead = movingXZ;
                            }
                            else if ((DateTime.UtcNow - cmdTime).TotalMilliseconds > motionStartTimeoutMs)
                            {
                                Console.WriteLine("Warning: no final motion observed after command");
                                break;
                            }
                        }
                        else
                        {
                            if (lastRead != null && movingXZ[0] == lastRead[0] && movingXZ[1] == lastRead[1])
                            {
                                stableCount++;
                                Console.WriteLine($"{stableCount}");
                            }
                            else
                            {
                                stableCount = 0;
                            }
                            lastRead = movingXZ;
                            if (!isMoving && stableCount >= requiredStable)
                            {
                                Console.WriteLine("Crane Stopped moving (final)");
                                break;
                            }
                        }

                        await Task.Delay(100);
                    }

                    pos++;
                    await master.WriteSingleCoilAsync(1, craneRight, true);
                    await Task.Delay(1500);
                    await master.WriteSingleCoilAsync(1, craneLift, false);
                    await Task.Delay(1500);
                    await master.WriteSingleCoilAsync(1, craneRight, false);
                    await Task.Delay(1000);

                    lastRead = null;
                    stableCount = 0;
                    motionSeen = false;

                    await master.WriteSingleRegisterAsync(1, cranePos, 55);
                    await Task.Delay(500);
                    while (!token.IsCancellationRequested)
                    {
                        bool[] movingXZ = await master.ReadInputsAsync(1, 11, 2);
                        bool isMoving = movingXZ[0] || movingXZ[1];

                        if (!motionSeen)
                        {
                            if (isMoving)
                            {
                                Console.WriteLine("motionSeen");
                                motionSeen = true;
                                stableCount = 0;
                                lastRead = movingXZ;
                            }
                            else if ((DateTime.UtcNow - cmdTime).TotalMilliseconds > motionStartTimeoutMs)
                            {
                                Console.WriteLine("Warning: no final motion observed after command");
                                break;
                            }
                        }
                        else
                        {
                            if (lastRead != null && movingXZ[0] == lastRead[0] && movingXZ[1] == lastRead[1])
                            {
                                stableCount++;
                                Console.WriteLine($"{stableCount}");
                            }
                            else
                            {
                                stableCount = 0;
                            }
                            lastRead = movingXZ;
                            if (!isMoving && stableCount >= requiredStable)
                            {
                                Console.WriteLine("Crane Stopped moving (final)");
                                break;
                            }
                        }

                        await Task.Delay(100);
                    }
                    craneReady = true;
                }
                await Task.Delay(10);
            }

        }


        public void StopProgram()
        {
            Reset();
        }
    }
}
