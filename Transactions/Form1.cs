using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Transactions
{
    public partial class Form1 : Form
    {
        private string connectionString = $"Data Source={Environment.MachineName};Initial Catalog=WelkinATP;Integrated Security=SSPI;";
        private string[] monthNames = new string[]
            {
                "January", "February", "March", "April",
                "May", "June", "July", "August",
                "September", "October", "November", "December"
            };
        private string month = "";
        private string year = "";
        private string strOutput = "";
        private string siteName = "";


        public Form1()
        {
            InitializeComponent();
            setDate();
            backgroundWorker.WorkerReportsProgress = true;

            btnCopy.Enabled = false;
            btnSave.Enabled = false;
        }

        //button clicks
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            month = (Array.IndexOf(monthNames, cbMonths.Text) + 1).ToString("d2");
            year = cbYear.Text;

            if (!backgroundWorker.IsBusy)
            {
                backgroundWorker.RunWorkerAsync();
            }
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            strOutput = string.Empty;
            txtOut.Text = string.Empty;
            progressBar.Value = 0;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FileName = cbMonths.Text+" ("+lblSiteName.Text+").txt";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    SaveToTxtFile(filePath, txtOut.Text);
                }
            }
        }
        private void btnCopy_Click(object sender, EventArgs e)
        {
            if(txtOut.Text != "")
            {
                try
                {
                    Clipboard.SetText(txtOut.Text);
                    btnCopy.Enabled = false;
                    btnCopy.Text = "Copied";
                }
                catch { }
            }
        }

        //functions
        //geting on & off times
        public static string GetFirstAndLastLineChars(string filePath)
        {
            if (!File.Exists(filePath))
                return "not found\tnot found\tnull";

            string firstChars = "";
            string lastChars = "";

            try
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);

                // Get the first line and its first 8 characters
                if (lines.Length > 0)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string firstLine = lines[i];
                        firstChars = firstLine.Length >= 8 ? firstLine.Substring(0, 8) : firstLine;
                        if (firstChars.Contains(":") && firstChars.Length == 8 && !firstChars.Contains(" ") && !firstChars.Contains("\t"))
                            break;
                    }
                }

                // Get the last line and its first 8 characters
                if (lines.Length > 1)
                {
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        string lastLine = lines[i];
                        lastChars = lastLine.Length >= 8 ? lastLine.Substring(0, 8) : lastLine;
                        if (lastChars.Contains(":") && lastChars.Length == 8 && !lastChars.Contains(" ") && !lastChars.Contains("\t"))
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                // Handle file reading errors, if any
                Console.WriteLine($"Error reading the file: {ex.Message}");
            }


            if (!(firstChars.Contains(":") && firstChars.Length == 8 && !firstChars.Contains(" ") && !firstChars.Contains("\t")) || !(lastChars.Contains(":") && lastChars.Length == 8 && !lastChars.Contains(" ") && !lastChars.Contains("\t")))
            {
                return "not found\tnot found\tnull";
            }


            try
            {
                TimeSpan duration = DateTime.Parse(lastChars) - DateTime.Parse(firstChars);
                return firstChars +"\t"+ lastChars + "\t" + duration.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Concatinate the first 8 characters of the first and last lines
            return firstChars +"\t"+ lastChars + "\t" +"null";
        }

        //ui year and month
        private void setDate()
        {
            DateTime currentDate = DateTime.Now;
            DateTime prevMonth = currentDate.AddMonths(-1);
            int month = prevMonth.Month;
            int year = prevMonth.Year;
            cbYear.Items.Clear();

            cbYear.Items.Add(year-3);
            cbYear.Items.Add(year-2);
            cbYear.Items.Add(year-1);
            cbYear.Items.Add(year);
            cbYear.Items.Add(year+1);
            cbMonths.DataSource = monthNames;

            cbYear.SelectedItem = year;
            cbMonths.SelectedItem = monthNames[month - 1];
        }

        //save to txt method
        private void SaveToTxtFile(string filePath, string text)
        {
            try
            {
                // Write the text to the file
                File.WriteAllText(filePath, text);
                MessageBox.Show("Text saved to file successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving the file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //background worker
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int noOfDays = DateTime.DaysInMonth(Convert.ToInt32(year), Convert.ToInt32(month));
            string totalMonthlyTran = "";

            for (int i = 1; i <= noOfDays; i++)
            {
                string date = year+"-"+month+"-"+i.ToString("d2");

                string day = i.ToString("d2");
                string logPath = $"C:\\V_Services\\data\\{year}{month}{day}\\Logfile.log"; ;
                string onOffTime = GetFirstAndLastLineChars(logPath);

                string cspName1 = "null";
                string cspName2 = "null";
                string cspTran1 = "null";
                string cspTran2 = "null";
                string totalDayWiseTran = "null";



                try
                {
                    //geting site name & monthly trans
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "Select Office_Name from fSettings";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    siteName = reader.GetString(0);
                                }
                            }
                        }
                        query = $"Select * from transactions where pay_dt >='{year}-{month}-01 00:00:00.000' and pay_dt <='{year}-{month}-{noOfDays} 23:59:59.999'";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int rows = 0;
                                while (reader.Read())
                                {
                                    rows++;
                                }
                                totalMonthlyTran = rows.ToString();
                                if(rows < 1)
                                {
                                    MessageBox.Show("No Transaction Found!", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    break;
                                }
                            }
                        }
                        conn.Close();
                    }



                    //geting csp name
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = $"select DISTINCT CSPName from Transactions where pay_dt >='{date} 00:00:00.000' and pay_dt <='{date} 23:59:59.999'";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            SqlDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                if (cspName1 != "null")
                                {
                                    cspName2 = reader.GetString(0);
                                    break;
                                }
                                else
                                    cspName1 = reader.GetString(0);
                            }
                        }
                        conn.Close();
                    }



                    //geting transactions by date & csp name
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        //csp 1
                        string query = $"Select * from transactions where pay_dt >='{date} 00:00:00.000' and pay_dt <='{date} 23:59:59.999' and CSPName='{cspName1}'";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int rows = 0;
                                while (reader.Read())
                                {
                                    rows++;
                                }
                                cspTran1 = rows.ToString();
                            }
                        }
                        //csp 2
                        query = $"Select * from transactions where pay_dt >='{date} 00:00:00.000' and pay_dt <='{date} 23:59:59.999' and CSPName='{cspName2}'";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int rows = 0;
                                while (reader.Read())
                                {
                                    rows++;
                                }
                                cspTran2 = rows.ToString();
                            }
                        }
                        //daywise
                        query = $"Select * from transactions where pay_dt >='{date} 00:00:00.000' and pay_dt <='{date} 23:59:59.999'";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                int rows = 0;
                                while (reader.Read())
                                {
                                    rows++;
                                }
                                totalDayWiseTran = rows.ToString();
                            }
                        }
                        conn.Close();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                strOutput += siteName + "\t" + date + "\t" + onOffTime + "\t" + cspName1 + "\t" + cspTran1 + "\t" + cspName2 + "\t" + cspTran2 + "\t" + totalDayWiseTran+ "\t" + totalMonthlyTran + Environment.NewLine;

                System.Threading.Thread.Sleep(1);

                // Report progress                
                int progressPercentage = (int)((double)i / noOfDays * 100);
                backgroundWorker.ReportProgress(progressPercentage);
            }

        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            txtOut.Text = strOutput;
            lblSiteName.Text = siteName;

            btnCopy.Enabled = true;
            btnCopy.Text = "Copy";
            btnSave.Enabled = true;
        }
    }
}
