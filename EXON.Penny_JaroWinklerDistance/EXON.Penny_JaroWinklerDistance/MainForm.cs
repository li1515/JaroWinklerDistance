using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Threading;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace EXON.Penny_JaroWinklerDistance
{
    public partial class MainForm : Form
    {
        private workers.ComparerWorker comparer = new workers.ComparerWorker();
        
        private ManualResetEvent resetEvent = new ManualResetEvent(false);
       
        private static bool m_continue = true;
        public static Boolean phase = false;

        //List<string> prox = new List<string>();
        Dictionary<string, string> ZP_AllCities = new Dictionary<string, string>();
        Dictionary<string, string> ZP_AllProx = new Dictionary<string, string>();

        Dictionary<string, string> ZP_AllStreets = new Dictionary<string, string>();
        Dictionary<string, string> ZP_AllSProx = new Dictionary<string, string>();


        public MainForm()
        {
            InitializeComponent();
            this.Text = Application.ProductName + " v" + Application.ProductVersion;
          
            
            comparer.WorkerReportsProgress = true;
            comparer.WorkerSupportsCancellation = true;

            comparer.ProgressChanged += comparer_ProgressChanged;
            comparer.RunWorkerCompleted += comparer_RunWorkerCompleted;
        }


        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void comparer_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            workers.ComparerWorker.Results res = (workers.ComparerWorker.Results)e.UserState;
            toolStripProgressBar1.Value = e.ProgressPercentage;

            
            dataGridView1.Rows.Add(res.ID, res.scannedName, res.newName, res.percentage, res.ZdrojObrazu, res.indexScanned, res.index1, res.streetScanned, res.street, res.flag);
            label5.Text = dataGridView1.RowCount.ToString();

            if (phase == false)
            {
                ZP_AllCities.Add(res.ZdrojObrazu.ToString(), res.scannedName.ToString());
                ZP_AllProx.Add(res.ZdrojObrazu.ToString(), res.percentage.ToString());
            }
            else
            {
                ZP_AllStreets.Add(res.ZdrojObrazu.ToString(), res.street.ToString());
                ZP_AllSProx.Add(res.ZdrojObrazu.ToString(), res.percentage.ToString());
            }

            if (phase == false && dataGridView1.RowCount == numericUpDown1.Value && radioButton8.Checked && checkBox1.Checked)
            {
                button1.PerformClick();
                buttonSave.PerformClick();
                ZP_AllCities.Clear();
                ZP_AllProx.Clear();
                button3.PerformClick();
                //MessageBox.Show("Flag for " + numericUpDown1.Value + " records was changed");

            }
            if (phase == true && dataGridView1.RowCount == numericUpDown1.Value && radioButton8.Checked && checkBox1.Checked)
            {
                button1.PerformClick();
                buttonSave.PerformClick();
                ZP_AllStreets.Clear();
                ZP_AllSProx.Clear();
                button3.PerformClick();
                //MessageBox.Show("Flag for " + numericUpDown1.Value + " records was changed");

            }
            if (dataGridView1.RowCount == numericUpDown1.Value && !radioButton8.Checked && !checkBox1.Checked)
            {
                button1.PerformClick();
                MessageBox.Show(numericUpDown1.Value + " records showed");
            }
            if (e.ProgressPercentage.ToString() == "100" && dataGridView1.RowCount < numericUpDown1.Value)
            {
                buttonSave.PerformClick();
                MessageBox.Show(dataGridView1.RowCount + " records found");
            }
           
        }

        void comparer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else
            {
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (radioButton10.Checked)
            { phase = true; }

            if (radioButton7.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range0, resetEvent = resetEvent });
            else if (radioButton1.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range1, resetEvent = resetEvent });
            else if (radioButton2.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range2, resetEvent = resetEvent });
            else if (radioButton3.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range3, resetEvent = resetEvent });
            else if (radioButton4.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range4, resetEvent = resetEvent });
            else if (radioButton5.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range5, resetEvent = resetEvent });
            else if (radioButton8.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range7, resetEvent = resetEvent });
            else if (radioButton6.Checked)
                comparer.RunWorkerAsync(new workers.ComparerWorker.Arguments() { cItem = enums.CompareItem.Range6, resetEvent = resetEvent });
            resetEvent.Set();
            
            buttonStart.Enabled =false;
        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
           

           

        }

        private void mnuCopy_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell.Value != null && dataGridView1.CurrentCell.ColumnIndex == 0)
            {

                string barcode = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                System.Windows.Forms.Clipboard.SetText(barcode);

            }
        }
        private void showPicture(string b, string z)
        {

            Process photoViewer = new Process();
            photoViewer.StartInfo.FileName = @"C:\\Program Files (x86)\\IrfanView\\i_view32.exe";
            
            string path1 = textBox1.Text.ToString() + b.Substring(0,6) +"\\"+ b.Substring(6,3)+"\\"+b+".TIF";
            string path2 = textBox2.Text.ToString() + z;
            if (File.Exists(path1))
            {
                photoViewer.StartInfo.Arguments = @path1;
                photoViewer.Start();
            }
            else if (File.Exists(path2))
            {
                photoViewer.StartInfo.Arguments = @path2;
                photoViewer.Start();
            }
            else 
            {
                MessageBox.Show("File is not found");
            }
        }
       

        private void button1_Click(object sender, EventArgs e)
        {

            if (comparer.IsBusy)
            {
                if (button1.Text.ToUpper() == "PAUSE")
                {
                    button1.Text = "Resume";
                    m_continue = false;
                    resetEvent.Reset();
                }
                else if (button1.Text.ToUpper() == "RESUME")
                {

                    button1.Text = "Pause";
                    m_continue = true;
                    resetEvent.Set();

                }
            }
            }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (phase == false)                              //cities
            {
                bool ifFlag = false;                        //checkbox to save flags only
                if (checkBox1.Checked == true)
                { ifFlag = true; }
                if (radioButton8.Checked && ifFlag == true)         //if prox =1 and change flag
                {
                    comparer.save100flag(ZP_AllCities, ZP_AllProx, phase);
                }
                else
                {
                    Dictionary<string, string> ZP_cities = new Dictionary<string, string>();
                    Dictionary<string, string> ZP_prox = new Dictionary<string, string>();
                    List<string> Proc = new List<string>();

                    for (int rows = 0; rows < dataGridView1.Rows.Count - 1; rows++)
                    {
                        if (Convert.ToBoolean(dataGridView1.Rows[rows].Cells[9].Value) == true)
                        {
                            ZP_cities.Add(dataGridView1.Rows[rows].Cells[4].Value.ToString(), dataGridView1.Rows[rows].Cells[2].Value.ToString());
                            ZP_prox.Add(dataGridView1.Rows[rows].Cells[4].Value.ToString(), dataGridView1.Rows[rows].Cells[3].Value.ToString());
                        }
                        else
                        {
                            Proc.Add(dataGridView1.Rows[rows].Cells[4].Value.ToString());
                        }
                    }
                    comparer.handleUnprocessed(Proc, ifFlag);
                    comparer.saveChanges(ZP_cities, ZP_prox, ifFlag);
                }
            }
            else
            {
                bool ifFlag = false;                                //checkbox to save flags only
                if (checkBox1.Checked == true)
                { ifFlag = true; }
                if (radioButton8.Checked && ifFlag == true)         //if prox =1 and change flag
                {
                    comparer.save100flag(ZP_AllStreets, ZP_AllSProx, phase);
                }
                else
                {
                    Dictionary<string, string> ZP_streets = new Dictionary<string, string>();
                    Dictionary<string, string> ZP_prox = new Dictionary<string, string>();
                    List<string> Proc = new List<string>();

                    for (int rows = 0; rows < dataGridView1.Rows.Count; rows++)
                    {



                        if (Convert.ToBoolean(dataGridView1.Rows[rows].Cells[9].Value) == true)
                        {
                            ZP_streets.Add(dataGridView1.Rows[rows].Cells[4].Value.ToString(), dataGridView1.Rows[rows].Cells[8].Value.ToString());
                            ZP_prox.Add(dataGridView1.Rows[rows].Cells[4].Value.ToString(), dataGridView1.Rows[rows].Cells[3].Value.ToString());
                        }
                        else
                        {
                            //Proc.Add(dataGridView1.Rows[rows].Cells[4].Value.ToString());
                        }
                    }
                    //comparer.handleUnprocessed(Proc);
                    comparer.saveChangesStreets(ZP_streets, ZP_prox, ifFlag);
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

           
        }


        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
                for (int rows = 0; rows < dataGridView1.Rows.Count; rows++)
                {

                    dataGridView1.Rows[rows].Cells[9].Value = true;

                }
            
            else { 
            for (int rows = 0; rows < dataGridView1.Rows.Count; rows++)
            {

                dataGridView1.Rows[rows].Cells[9].Value = false;

            }
                    }
        }

        private void checkBox2_CheckStateChanged(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            if (button1.Text.ToUpper() == "RESUME")
            {

                button1.Text = "Pause";
                m_continue = true;
                resetEvent.Set();

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value = numericUpDown1.Maximum;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Are you sure?", "Add Confirmation", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)  // error is here
            {
                comparer.resetProcessed();
            }
            else
            {
                
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell.Value != null && dataGridView1.CurrentCell.ColumnIndex == 0)
            {

                string barcode = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                string zdroj = dataGridView1.CurrentRow.Cells[4].Value.ToString();
                showPicture(barcode, zdroj);

            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
        


        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            dataGridView1.ClipboardCopyMode =

                DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            if (e.Button == MouseButtons.Right)
            {

                ContextMenu m = new ContextMenu();
                m.MenuItems.Add(new MenuItem("Copy"));
                int currentMouseOverRow = dataGridView1.HitTest(e.X, e.Y).RowIndex;
                m.Show(dataGridView1, new Point(e.X, e.Y));
                if (dataGridView1.GetCellCount(DataGridViewElementStates.Selected) > 0)
                {
                    try
                    {
                        // Add the selection to the clipboard.
                        Clipboard.SetDataObject(
                            dataGridView1.GetClipboardContent());
                        // Replace the text box contents with the clipboard text. 

                        //MessageBox.Show(Clipboard.GetText());
                    }
                    catch (System.Runtime.InteropServices.ExternalException)
                    {

                        MessageBox.Show("The Clipboard could not be accessed. Please try again.");
                    }
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string phase = panel3.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked).Text;
            DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Are you sure you want to clear flag for all " + phase + "?", "Add Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)  // error is here
            {
                comparer.resetFlag(phase);
            }
            else
            {

            }
        }
        public void ResetAndNext()
        {
            ZP_AllCities.Clear();
            ZP_AllProx.Clear();
            button3.PerformClick();
        }

      

        }

      
    }

