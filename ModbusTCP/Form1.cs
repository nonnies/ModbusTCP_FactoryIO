using NModbus;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModbusTCP
{

    public partial class Form1 : Form
    {
        // keep palletizer instance so we can stop it later
        private Palletizer _palletizer = new Palletizer();
        CancellationTokenSource _cts = new CancellationTokenSource();
        public Form1()
        {
            InitializeComponent();

            // subscribe to count updates from ModbusTcp
            Conveyor.CountUpdated += ModbusTcp_CountUpdated;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "START")
            {
                btnStart.Text = "STOP";
                btnStart.BackColor = Color.Red;
                lblStatus.Text = "Status : Running";
                ModbusTcp.ModbusTcpConnection("START");
            }
            else
            {
                btnStart.Text = "START";
                btnStart.BackColor = Color.LimeGreen;
                lblStatus.Text = "Status : Stopped";
                ModbusTcp.ModbusTcpConnection("STOP");
            }
        }



        private void btnSetval_Click(object sender, EventArgs e)
        {
            if (lblStatus.Text == "Status : Running")
            {
                if (cbbRegisterType.SelectedItem != null)
                {
                    if (cbbRegisterType.SelectedItem.ToString() != "Holding Register")
                    {
                        MessageBox.Show("Please select a valid register type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (string.IsNullOrEmpty(txtRegAdr.Text) == true)
                    {
                        MessageBox.Show("Please enter a valid register address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (string.IsNullOrEmpty(txtRegVal.Text) == true)
                    {
                        MessageBox.Show("Please enter a valid register value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (cbbRegisterType.SelectedItem.ToString() == "Holding Register" && !string.IsNullOrEmpty(txtRegAdr.Text) && !string.IsNullOrEmpty(txtRegVal.Text))
                    {
                        ushort registerAddress = Convert.ToUInt16(txtRegAdr.Text);
                        ushort value = ushort.Parse(txtRegVal.Text);
                        ModbusTcp.ModbusTcpMasterWriteSingleRegister(registerAddress, value);
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid register type or enter valid register address and value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a register type from the dropdown list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please start the connection first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnViewIn_Click(object sender, EventArgs e)
        {
            TextBox[] textboxes = { textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7, textBox8, textBox9, textBox10 };
            
            foreach (var textbox in textboxes) 
            {
                textbox.Clear();
            }

            if (lblStatus.Text == "Status : Running")
            {
                if (cbbRegisterType.SelectedItem != null)
                {
                    if (cbbRegisterType.SelectedItem.ToString() == "Digital Inputs")
                    {
                        ushort registerAddress = Convert.ToUInt16(txtRegAdr.Text);
                        bool[] inputValues = ModbusTcp.ModbusTcpMasterReadInputs(registerAddress);
                        
                        for (int i = 0; i < inputValues.Length; i++)
                        {
                            textboxes[i+5].AppendText(inputValues[i].ToString());
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            textboxes[i].AppendText($"input {registerAddress + i}");
                        }  
                    } 
                    else if (cbbRegisterType.SelectedItem.ToString() == "Holding Register")
                    {
                        ushort registerAddress = Convert.ToUInt16(txtRegAdr.Text);
                        ushort[] Values = ModbusTcp.ModbusTcpMasterReadHoldingRegisters(registerAddress);

                        for (int i = 0; i < Values.Length; i++)
                        {
                            textboxes[i + 5].AppendText(Values[i].ToString());
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            textboxes[i].AppendText($"input {registerAddress + i}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a register type from the dropdown list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please start the connection first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSetCoil_Click(object sender, EventArgs e)
        {
            if (lblStatus.Text == "Status : Running")
            {
                if (cbbRegisterType.SelectedItem != null)
                {
                    if (cbbRegisterType.SelectedItem.ToString() != "Digital Inputs")
                    {
                        MessageBox.Show("Please select a valid register type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (string.IsNullOrEmpty(txtRegAdr.Text) == true)
                    {
                        MessageBox.Show("Please enter a valid register address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (string.IsNullOrEmpty(txtRegVal.Text) == true)
                    {
                        MessageBox.Show("Please enter a valid register value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (cbbRegisterType.SelectedItem.ToString() == "Digital Inputs" && !string.IsNullOrEmpty(txtRegAdr.Text) && !string.IsNullOrEmpty(txtRegVal.Text))
                    {
                        ushort registerAddress = Convert.ToUInt16(txtRegAdr.Text);

                        string text = txtRegVal.Text?.Trim();
                        bool value;

                        // First try normal boolean literals, then accept numeric 0/1
                        if (!bool.TryParse(text, out value))
                        {
                            if (int.TryParse(text, out int n))
                            {
                                if (n == 0) value = false;
                                else if (n == 1) value = true;
                                else
                                {
                                    MessageBox.Show("Please enter a valid boolean value (true/false or 0/1).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Please enter a valid boolean value (true/false or 0/1).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        ModbusTcp.ModbusTcpMasterWriteSingleCoil(registerAddress, value);
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid register type or enter valid register address and value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a register type from the dropdown list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please start the connection first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ModbusTcp_CountUpdated(ushort count)
        {
            // marshal update to UI thread
            if (lblCount.InvokeRequired)
            {
                lblCount.BeginInvoke((Action)(() => lblCount.Text = count.ToString()));
            }
            else
            {
                lblCount.Text = count.ToString();
            }
        }

        private async void btnStartProgram_Click(object sender, EventArgs e)
        {
            if(lblStatus.Text == "Status : Running")
            {
                btnStartProgram.Enabled = false;
                await Task.Run(() => { Conveyor.BoxAssembler(); } );
            }
            else
            {
                
                MessageBox.Show("Please start the connection first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btnStartProgram.Enabled = true;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            Conveyor.Reset();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            // request the Modbus loop to stop
            Conveyor.RequestStop();

        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // unsubscribe to avoid leaks
            Conveyor.CountUpdated -= ModbusTcp_CountUpdated;
            base.OnFormClosed(e);
        }

        private async void btnStartProgram2_Click(object sender, EventArgs e)
        {
            _cts = new CancellationTokenSource();
            _palletizer = new Palletizer();

            Task _boxFeederTask =  _palletizer.BoxFeederAsync(_cts.Token);
            Task _palletFeederTask = _palletizer.PalletFeederAsync(_cts.Token);
            Task _turnableTask = _palletizer.TurnableAsync(_cts.Token);
            Task _entryCraneTask = _palletizer.CraneEntryAsync(_cts.Token);
            Task _craneTask = _palletizer.CraneAsync(_cts.Token); 

            await Task.WhenAll(_boxFeederTask, _palletFeederTask, _turnableTask, _craneTask, _entryCraneTask);
        }

        private void btnStop2_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            _palletizer?.StopProgram();

            _cts?.Dispose();
            _cts = null;
        }
    }
}
