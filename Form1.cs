using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Net;
using SuperSimpleTcp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using ClosedXML.Excel;
using System.Net.NetworkInformation;

namespace FlowMeterLoger
{
    public partial class Form1 : Form
    {
        SimpleTcpServer server;
        MySqlConnection DBConnection = new MySqlConnection("server=localhost; uid=root; database=logflowmeter");
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                NetworkInterface ethernet2 = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic => nic.Name == "Ethernet 2");
                if (ethernet2 != null)
                {
                    var props = ethernet2.GetIPProperties().UnicastAddresses;
                    foreach (var prop in props)
                    {
                        if (prop.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            string localIP = prop.Address.ToString();
                            // gunakan localIP untuk melakukan tindakan yang Anda perlukan
                            server = new SimpleTcpServer($"{localIP}:6789");
                            toolStripStatusLabel1.Text = "Start Server " + localIP;
                            if (localIP == "127.0.0.1")
                            {
                                MessageBox.Show("NETWORK ERROR: Tidak terhubung ke jaringan. Harap periksa koneksi Wifi atau Ethernet", "TCP WARNING COMMUNICATION", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Application.Exit();
                            }
                            break;
                        }
                    }
                }
                else
                {
                    IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                    bool ipFound = false;
                    foreach (IPAddress ipAddress in ipAddresses)
                    {
                        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            string localIP = ipAddress.ToString();
                            server = new SimpleTcpServer($"{localIP}:6789");
                            toolStripStatusLabel1.Text = "Start Server " + localIP;
                            if (localIP == "127.0.0.1")
                            {
                                MessageBox.Show("NETWORK ERROR: Tidak terhubung ke jaringan. Harap periksa koneksi Wifi atau Ethernet", "TCP WARNING COMMUNICATION", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Application.Exit();
                            }
                            ipFound = true;
                            break;
                        }
                    }
                    if (!ipFound)
                    {
                        MessageBox.Show("NETWORK ERROR: Tidak ditemukan IP Address yang valid", "TCP WARNING COMMUNICATION", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                }

                server.Start();
                server.Events.ClientConnected += Events_ClientConnected;
                server.Events.ClientDisconnected += Events_ClientDisconnected;
                server.Events.DataReceived += Events_DataReceived;

            }
            catch (Exception ex)
            {
                MessageBox.Show("NETWORK ERROR: " + ex.Message, "TCP ERROR COMMUNICATION", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            tableLayoutPanel7.Height = tableLayoutPanel6.Height;
            dateTimePicker2.Value = DateTime.Now;
            dateTimePicker1.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 1, 0, 0);
            button_onoff1.BackColor = Color.Red;
            button_onoff2.BackColor = Color.Red;
            button_onoff3.BackColor = Color.Red;
            button_onoff4.BackColor = Color.Red;
            label_totalair.Text = "Total Air : 0 m³";
            show_DBtoDGV();
        }
        private String IpPort_Connect = String.Empty;
        private void Events_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            IpPort_Connect = e.IpPort;

            if (!string.IsNullOrEmpty(InputData))
            {
                Invoke(new Action(() => toolStripStatusLabel1.Text = $"{IpPort_Connect}:Disconnected"));
            }
        }

        private void Events_ClientConnected(object sender, ConnectionEventArgs e)
        {
            IpPort_Connect = e.IpPort;

            if (!string.IsNullOrEmpty(InputData))
            {
                Invoke(new Action(() => toolStripStatusLabel1.Text = $"{IpPort_Connect}:Connected"));
            }
        }

        private string InputData = String.Empty;
        private string InputIpPort = String.Empty;
        private void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            InputData = Encoding.UTF8.GetString(e.Data.ToArray());
            InputIpPort = e.IpPort;

            if (!string.IsNullOrEmpty(InputData))
            {
                this.Invoke(new EventHandler(TerimadataTCP));
            }
        }

        public static string BeautyJson(string jsonstr)
        {
            JToken parsejson = JToken.Parse(jsonstr);
            return parsejson.ToString(Formatting.Indented);
        }

        private double total_air = 0.0f;
        private Int32 devID_flow1 = 1, devID_flow2 = 2, devID_flow3 = 3, devID_flow4 = 4;
        private string dev1_IPPort = string.Empty, dev2_IPPort = string.Empty, dev3_IPPort = string.Empty, dev4_IPPort = string.Empty;
        private void TerimadataTCP(object sender, EventArgs e)
        {
            try
            {
                JSON_FORMAT_TCP pesan = JsonConvert.DeserializeObject<JSON_FORMAT_TCP>(InputData);
                Int32 devID = pesan.devID;
                string buttonOnOffText = pesan.onoff ? "ON" : "OFF";
                Color buttonOnOffBackColor = pesan.onoff ? Color.Green : Color.Red;
                string mode_auto_manual = string.Empty;
                if (pesan.mode == 1)
                {
                    mode_auto_manual = "Manual";
                }
                else if (pesan.mode == 2)
                {
                    mode_auto_manual = "Automatis";
                }
                switch (devID)
                {
                    case 1:
                        dev1_IPPort = InputIpPort;
                        devID_flow1 = pesan.devID;
                        UpdateValueFlowmeter(label_Voulme1, numericUpDown_setliter1, label_FlowRate1, radioButton_connect1, Math.Round(pesan.vol, 1).ToString(), (decimal)Math.Round(pesan.setvol, 1), Math.Round(pesan.LPM, 1).ToString());
                        connectionCounter1 = 0;

                        button_onoff1.Text = buttonOnOffText;
                        button_onoff1.BackColor = buttonOnOffBackColor;

                        if (pesan.log)
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                insertDataBase_Log(devID_flow1, Math.Round(pesan.LPM, 1), Math.Round(pesan.setvol, 1), Math.Round(pesan.vol, 1), mode_auto_manual);
                            }));
                        }
                        break;
                    case 2:
                        dev2_IPPort = InputIpPort;
                        devID_flow2 = pesan.devID;
                        UpdateValueFlowmeter(label_Voulme2, numericUpDown_setliter2, label_FlowRate2, radioButton_connect2, Math.Round(pesan.vol, 1).ToString(), (decimal)Math.Round(pesan.setvol, 1), Math.Round(pesan.LPM, 1).ToString());
                        connectionCounter2 = 0;

                        button_onoff2.Text = buttonOnOffText;
                        button_onoff2.BackColor = buttonOnOffBackColor;

                        if (pesan.log)
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                insertDataBase_Log(devID_flow2, Math.Round(pesan.LPM, 1), Math.Round(pesan.setvol, 1), Math.Round(pesan.vol, 1), mode_auto_manual);
                            }));
                        }
                        break;
                    case 3:
                        dev3_IPPort = InputIpPort;
                        devID_flow3 = pesan.devID;
                        UpdateValueFlowmeter(label_Voulme3, numericUpDown_setliter3, label_FlowRate3, radioButton_connect3, Math.Round(pesan.vol, 1).ToString(), (decimal)Math.Round(pesan.setvol, 1), Math.Round(pesan.LPM, 1).ToString());
                        connectionCounter3 = 0;

                        button_onoff3.Text = buttonOnOffText;
                        button_onoff3.BackColor = buttonOnOffBackColor;

                        if (pesan.log)
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                insertDataBase_Log(devID_flow3, Math.Round(pesan.LPM, 1), Math.Round(pesan.setvol, 1), Math.Round(pesan.vol, 1), mode_auto_manual);
                            }));
                        }
                        break;
                    case 4:
                        dev4_IPPort = InputIpPort;
                        devID_flow4 = pesan.devID;
                        UpdateValueFlowmeter(label_Voulme4, numericUpDown_setliter4, label_FlowRate4, radioButton_connect4, Math.Round(pesan.vol, 1).ToString(), (decimal)Math.Round(pesan.setvol, 1), Math.Round(pesan.LPM, 1).ToString());
                        connectionCounter4 = 0;

                        button_onoff4.Text = buttonOnOffText;
                        button_onoff4.BackColor = buttonOnOffBackColor;

                        if (pesan.log)
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                insertDataBase_Log(devID_flow4, Math.Round(pesan.LPM, 1), Math.Round(pesan.setvol, 1), Math.Round(pesan.vol, 1), mode_auto_manual);
                            }));
                        }
                        break;
                }
            }
            catch (JsonException ex)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    toolStripStatusLabel1.Text = "Terima DATA : Error deserializing JSON data" + ex.Message;
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    toolStripStatusLabel1.Text = "Terima DATA : " + ex.Message;
                }));
            }
        }
        private void UpdateValueFlowmeter(Label label, NumericUpDown numericUpDown, Label label1, RadioButton radioButton, string volume_flow, decimal setliter, string flowrate)
        {
            label.Text = volume_flow;
            numericUpDown.Value = setliter;
            label1.Text = flowrate;
            radioButton.Checked = true;
        }

        private void insertDataBase_Log(int dBid_flow, double dBlpm, double dBsetvol, double dBvolume, string _mode)
        {
            try
            {
                DBConnection.Open();
                MySqlCommand cmd;
                cmd = DBConnection.CreateCommand();
                cmd.CommandText = "INSERT INTO logdata(IDFlow, FlowRate, SetVolume, Volume, Mode, DateTime) VALUE(@IDFlow, @FlowRate, @SetVolume, @Volume, @Mode, @DateTime)";
                cmd.Parameters.AddWithValue("@IDFlow", dBid_flow);
                cmd.Parameters.AddWithValue("@FlowRate", dBlpm);
                cmd.Parameters.AddWithValue("@SetVolume", dBsetvol);
                cmd.Parameters.AddWithValue("@Volume", dBvolume);
                cmd.Parameters.AddWithValue("@Mode", _mode);
                cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
                cmd.ExecuteNonQuery();
                DBConnection.Close();
                dataGridView1.Columns.Clear();
                show_DBtoDGV();
            }
            catch (Exception ex)
            {
                MessageBox.Show(("Insert database warning :") + ex.Message.ToString(), "DATABASE MYSQL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void show_DBtoDGV()
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                DataTable dataTable = new DataTable();
                string cmdselect = string.Empty;
                if (checkBox_alldata.Checked == true)
                {
                    cmdselect = "SELECT * FROM logdata";
                }
                else
                {
                    if (numeric_select_flow.Value == 0)
                    {
                        cmdselect = "SELECT * FROM logdata WHERE DateTime BETWEEN @startDate AND @endDate";
                    }
                    else
                    {
                        cmdselect = "SELECT * FROM logdata WHERE DateTime BETWEEN @startDate AND @endDate AND IDFlow = @numid";
                    }
                }
                try
                {
                    using (MySqlCommand command = new MySqlCommand(cmdselect, DBConnection))
                    {
                        command.Parameters.Add("@startDate", MySqlDbType.DateTime).Value = dateTimePicker1.Value.Date;
                        command.Parameters.Add("@endDate", MySqlDbType.DateTime).Value = dateTimePicker2.Value.Date.AddDays(1).AddSeconds(-1);

                        if (numeric_select_flow.Value != 0)
                        {
                            command.Parameters.AddWithValue("@numid", numeric_select_flow.Value);
                        }

                        DBConnection.Open();

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }

                        DBConnection.Close();

                        dataGridView1.DataSource = dataTable;
                        total_air = 0.0f;
                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            if (row.Cells[4].Value != null)
                            {
                                double airValue;
                                if (double.TryParse(row.Cells[4].Value.ToString(), out airValue))
                                {
                                    total_air += airValue;
                                }
                            }
                        }

                        label_totalair.Text = $"Total Air : {Math.Round(total_air / 1000, 4)} m\u00B3";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(("Open database warning :") + ex.Message.ToString(), "DATABASE MYSQL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (export_xl_enable == true)
                {
                    ExportToExcel(dataTable);
                    export_xl_enable = false;
                }
                else
                {
                    //get distinct IDFlow values
                    var idFlowValues = dataTable.AsEnumerable()
                        .Select(row => row.Field<int>("IDFlow"))
                        .Distinct();

                    //create a list to hold all unique data
                    var allData = new List<DataRow>();
                    foreach (int idFlowValue in idFlowValues)
                    {
                        //filter data using IDFlow value
                        var filteredData = dataTable.Select("IDFlow = " + idFlowValue);
                        allData.AddRange(filteredData);
                    }

                    //clear all existing series
                    chart1.Series.Clear();

                    //add series for each IDFlow value
                    foreach (int idFlowValue in idFlowValues)
                    {
                        string seriesName = "Flow" + idFlowValue.ToString();
                        var series = chart1.Series.Add(seriesName);
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                        series.BorderWidth = 2;
                        series.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
                        series.MarkerSize = 6;
                        series.XValueMember = "ID";
                        series.YValueMembers = "Volume";
                        series.Name = seriesName;

                        //filter data using IDFlow value
                        var filteredData = allData.Where(row => row.Field<int>("IDFlow") == idFlowValue).ToList();
                        series.Points.DataBind(filteredData, "ID", "Volume", "");
                    }

                    //set axis titles
                    chart1.ChartAreas[0].AxisY.Title = "Volume (Liter)";

                    //add legend if it doesn't exist
                    if (chart1.Legends.IndexOf("Legend") == -1) //cek apakah legend belum ada di koleksi
                    {
                        chart1.Legends.Add("Legend");
                        chart1.Legends[0].Enabled = true;
                        chart1.Legends[0].Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
                    }

                    //rebind the chart
                    chart1.Invalidate();
                    chart1.Refresh();

                    //get total volume for all data
                    double totalVolume = dataTable.AsEnumerable().Sum(row => row.Field<double>("Volume"));

                    //create a dictionary to hold volume for each IDFlow value
                    var idFlowVolumes = new Dictionary<int, double>();
                    foreach (int idFlowValue in idFlowValues)
                    {
                        //calculate total volume for each IDFlow value
                        double idFlowVolume = allData.Where(row => row.Field<int>("IDFlow") == idFlowValue)
                        .Sum(row => row.Field<double>("Volume"));
                        idFlowVolumes.Add(idFlowValue, idFlowVolume);
                    }

                    //clear all existing series
                    chart2.Series.Clear();

                    //add series for total volume and each IDFlow volume
                    var seriesIDFlow = chart2.Series.Add("IDFlow Volume");
                    seriesIDFlow.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Pie;
                    seriesIDFlow.BorderWidth = 1;
                    seriesIDFlow.IsVisibleInLegend = true;
                    seriesIDFlow.LegendText = "#VALX (#PERCENT{P0})";

                    //loop through each IDFlow value and add data point to IDFlow volume series
                    foreach (int idFlowValue in idFlowValues)
                    {
                        string seriesName = "Flow" + idFlowValue.ToString();
                        double idFlowVolume = idFlowVolumes[idFlowValue];
                        double idFlowPercentage = idFlowVolume / totalVolume * 100;
                        seriesIDFlow.Points.AddXY(seriesName, idFlowVolume);
                        seriesIDFlow.Points.Last().Label = "#PERCENT{P0}";
                        seriesIDFlow.Points.Last().ToolTip = seriesName + ": " + idFlowVolume.ToString() + " Liter (" + idFlowPercentage.ToString("F2") + "%)";
                    }

                    //set axis titles
                    chart2.ChartAreas[0].AxisY.Title = "Volume (Liter)";

                    //add legend if it doesn't exist
                    if (chart2.Legends.IndexOf("Legend") == -1) //cek apakah legend belum ada di koleksi
                    {
                        chart2.Legends.Add("Legend");
                        chart2.Legends[0].Enabled = true;
                        chart2.Legends[0].Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
                    }

                    //rebind the chart
                    chart2.Invalidate();
                    chart2.Refresh();
                }
            }));
        }
        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var hitTestResult = chart1.HitTest(e.X, e.Y);
            if (hitTestResult.ChartElementType == ChartElementType.DataPoint)
            {
                var dataPoint = hitTestResult.Series.Points[hitTestResult.PointIndex];
                var volume = dataPoint.YValues[0];
                var markerSize = dataPoint.MarkerSize;
                var seriesName = hitTestResult.Series.Name;
                var pointIndex = hitTestResult.PointIndex;

                // Menambahkan efek highlight pada marker dan menampilkan tooltip volume
                chart1.Series[seriesName].Points[pointIndex].MarkerSize = markerSize + 4;
                chart1.Series[seriesName].Points[pointIndex].MarkerColor = Color.Red;
                chart1.Series[seriesName].ToolTip = $"{volume:N} Liter";
            }
            else
            {
                foreach (var series in chart1.Series)
                {
                    // Mengembalikan ukuran dan warna marker ke semula
                    series.Points.ToList().ForEach(p =>
                    {
                        p.MarkerSize = 6;
                        p.MarkerColor = series.Color;
                    });

                    series.ToolTip = ""; //menyembunyikan tooltip
                }
            }
        }

        private void button_submit_Click(object sender, EventArgs e)
        {
            show_DBtoDGV();
        }

        private void checkBox_alldata_CheckedChanged(object sender, EventArgs e)
        {
            bool isAllDataSelected = checkBox_alldata.Checked;
            dateTimePicker1.Enabled = !isAllDataSelected;
            dateTimePicker2.Enabled = !isAllDataSelected;
            numeric_select_flow.Enabled = !isAllDataSelected;
        }
        private bool export_xl_enable = false;
        private void button_Export_Click(object sender, EventArgs e)
        {
            export_xl_enable = true;
            show_DBtoDGV();
        }

        private void ExportToExcel(DataTable dataTable)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Excel Workbook|*.xlsx" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add(dataTable, "logdata");
                            int i = 0;
                            for (i = 0; i < Math.Min(dataTable.Rows.Count, 1048576); i++)
                            { 
                                i += 2; 
                            }
                            worksheet.Cell(i+1, 4).Value = "Total";
                            worksheet.Cell(i+1, 5).Value = Math.Round(total_air, 4);
                            workbook.SaveAs(sfd.FileName);
                        }
                        MessageBox.Show("Data berhasil diekspor ke excel", "INFORMASI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(("exspor data excel :") + ex.Message.ToString(), "EXPORT DATA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private const int MAX_CONNECTION_COUNT = 90;
        private const int MIN_CONNECTION_COUNT = 50;
        private int connectionCounter1 = 0, connectionCounter2 = 0, connectionCounter3 = 0, connectionCounter4 = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (radioButton_connect1.Checked && connectionCounter1 < MAX_CONNECTION_COUNT)
            {
                connectionCounter1++;
                if (connectionCounter1 >= MIN_CONNECTION_COUNT && connectionCounter1 <= MAX_CONNECTION_COUNT)
                {
                    UpdateConnectionStatus(radioButton_connect1, button_onoff1, "OFF", Color.Red);
                    connectionCounter1 = MAX_CONNECTION_COUNT;
                }
            }
            else if (!radioButton_connect1.Checked)
            {
                connectionCounter1 = 0;
            }

            if (radioButton_connect2.Checked && connectionCounter2 < MAX_CONNECTION_COUNT)
            {
                connectionCounter2++;
                if (connectionCounter2 >= MIN_CONNECTION_COUNT && connectionCounter2 <= MAX_CONNECTION_COUNT)
                {
                    UpdateConnectionStatus(radioButton_connect2, button_onoff2, "OFF", Color.Red);
                    connectionCounter2 = MAX_CONNECTION_COUNT;
                }
            }
            else if (!radioButton_connect2.Checked)
            {
                connectionCounter2 = 0;
            }

            if (radioButton_connect3.Checked && connectionCounter3 < MAX_CONNECTION_COUNT)
            {
                connectionCounter3++;
                if (connectionCounter3 >= MIN_CONNECTION_COUNT && connectionCounter3 <= MAX_CONNECTION_COUNT)
                {
                    UpdateConnectionStatus(radioButton_connect3, button_onoff3, "OFF", Color.Red);
                    connectionCounter3 = MAX_CONNECTION_COUNT;
                }
            }
            else if (!radioButton_connect3.Checked)
            {
                connectionCounter3 = 0;
            }

            if (radioButton_connect4.Checked && connectionCounter4 < MAX_CONNECTION_COUNT)
            {
                connectionCounter4++;
                if (connectionCounter4 >= MIN_CONNECTION_COUNT && connectionCounter4 <= MAX_CONNECTION_COUNT)
                {
                    UpdateConnectionStatus(radioButton_connect4, button_onoff4, "OFF", Color.Red);
                    connectionCounter4 = MAX_CONNECTION_COUNT;
                }
            }
            else if (!radioButton_connect4.Checked)
            {
                connectionCounter4 = 0;
            }
        }

        private void UpdateConnectionStatus(RadioButton radioButton, Button button, string buttonText, Color buttonColor)
        {
            radioButton.Checked = false;
            button.Text = buttonText;
            button.BackColor = buttonColor;
        }
        private async void button_onoff1_Click(object sender, EventArgs e)
        {
            JSON_FORMAT_SEND data_Send = new JSON_FORMAT_SEND
            {
                devID = devID_flow1,
                onoff = false,
                setvol = Convert.ToDouble(numericUpDown_setliter1.Value),
            };

            await SendTcptoClientAsync(dev1_IPPort, JsonConvert.SerializeObject(data_Send));
        }

        private async void button_onoff2_Click(object sender, EventArgs e)
        {
            JSON_FORMAT_SEND data_Send = new JSON_FORMAT_SEND
            {
                devID = devID_flow2,
                onoff = false,
                setvol = Convert.ToDouble(numericUpDown_setliter2.Value),
            };

            await SendTcptoClientAsync(dev2_IPPort, JsonConvert.SerializeObject(data_Send));
        }

        private async void button_onoff3_Click(object sender, EventArgs e)
        {
            JSON_FORMAT_SEND data_Send = new JSON_FORMAT_SEND
            {
                devID = devID_flow3,
                onoff = false,
                setvol = Convert.ToDouble(numericUpDown_setliter3.Value),
            };

            await SendTcptoClientAsync(dev3_IPPort, JsonConvert.SerializeObject(data_Send));
        }

        private async void button_onoff4_Click(object sender, EventArgs e)
        {
            JSON_FORMAT_SEND data_Send = new JSON_FORMAT_SEND
            {
                devID = devID_flow4,
                onoff = false,
                setvol = Convert.ToDouble(numericUpDown_setliter4.Value),
            };

            await SendTcptoClientAsync(dev4_IPPort, JsonConvert.SerializeObject(data_Send));
        }

        private async Task SendTcptoClientAsync(string ipPort_Client, string msg)
        {
            if (server.IsListening)
            {
                try
                {
                    await server.SendAsync(ipPort_Client, msg);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("SEND DATA : " + ex.Message, "TCP WARNING COMMUNICATION", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            ResizeTableLayoutPanel();
        }

        private void ResizeTableLayoutPanel()
        {
            tableLayoutPanel7.Height = tableLayoutPanel6.Height;
        }
    }
}
