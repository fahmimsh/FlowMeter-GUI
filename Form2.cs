using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MySql.Data.MySqlClient;
using ClosedXML.Excel;
using System.IO;

namespace FlowMeterFactory
{
    public partial class Form2 : Form
    {
        private string connectionString = "server=localhost;database=logflowmeter;uid=root;";
        private string[] flowmeters;
        private string[] modes = { "Auto", "Manual" }, modeChart = { "Day", "Month" };
        private string[] tanks;
        private string[] sources; bool exportExc;
        public Form2()
        {
            InitializeComponent();
            // set default interval date pada dateTimePicker
            dateTimePicker1.Value = DateTime.Now.AddDays(-7);
            dateTimePicker2.Value = DateTime.Now;

            // ambil item-item dari Form1 untuk combobox comboFLmeter, comboTank, comboSrc
            flowmeters = (string[])Form1.tagFlowmeter;
            tanks = (string[])Form1.tagTangk;
            sources = (string[])Form1.tagSource;

            // tambahkan item-item ke dalam combobox
            comboFLmeter.Items.AddRange(flowmeters);
            comboMode.Items.AddRange(modes);
            comboTank.Items.AddRange(tanks);
            comboSrc.Items.AddRange(sources);
            comboChart.Items.AddRange(modeChart);

            // atur nilai default combobox
            comboFLmeter.SelectedIndex = -1;
            comboMode.SelectedIndex = -1;
            comboTank.SelectedIndex = -1;
            comboSrc.SelectedIndex = -1;
            comboChart.SelectedIndex = 0;
            dateTimePicker2.Value = DateTime.Now;
            dateTimePicker1.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 1, 0, 0);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            labelM3.Text = "m\u00B3";
            updateTabel();
            ChartLoger();
        }

        private void buttonSubmit_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            ChartLoger();
            updateTabel();
        }

        private void checkBoxalldata_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxalldata.Checked)
            {
                comboFLmeter.Enabled = false;
                comboMode.Enabled = false;
                comboTank.Enabled = false;
                comboSrc.Enabled = false;
            }
            else
            {
                comboFLmeter.Enabled = true;
                comboMode.Enabled = true;
                comboTank.Enabled = true;
                comboSrc.Enabled = true;
            }
        }
        private double total_air = 0.0f;
        private void updateTabel()
        {
            string query = "SELECT * FROM logdata WHERE DateTime BETWEEN @startDate AND @endDate";
            List<MySqlParameter> parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@startDate", dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss")),
                new MySqlParameter("@endDate", dateTimePicker2.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"))
            };

            if (!checkBoxalldata.Checked)
            {
                if (comboFLmeter.SelectedIndex != -1)
                {
                    query += " AND FlowMeter = @FlowMeter";
                    parameters.Add(new MySqlParameter("@FlowMeter", flowmeters[comboFLmeter.SelectedIndex]));
                }

                if (comboMode.SelectedIndex != -1)
                {
                    query += " AND Mode = @Mode";
                    parameters.Add(new MySqlParameter("@Mode", modes[comboMode.SelectedIndex]));
                }

                if (comboTank.SelectedIndex != -1)
                {
                    query += " AND ToTank = @ToTank";
                    parameters.Add(new MySqlParameter("@ToTank", tanks[comboTank.SelectedIndex]));
                }

                if (comboSrc.SelectedIndex != -1)
                {
                    query += " AND FromSource = @FromSource";
                    parameters.Add(new MySqlParameter("@FromSource", sources[comboSrc.SelectedIndex]));
                }
            }

            DataTable table = new DataTable();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        connection.Open();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(table);
                            dataGridView1.DataSource = table;
                            total_air = table.AsEnumerable().Sum(row => row.Field<double?>("Volume") ?? 0.0);
                            numericTotalLiter.Value = (decimal)(total_air / 1000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            //get distinct IDFlow values
            var idFlowValues = table.AsEnumerable()
                .Select(row => row.Field<int>("IDFlow"))
                .Distinct();

            //create a list to hold all unique data
            var allData = new List<DataRow>();
            foreach (int idFlowValue in idFlowValues)
            {
                //filter data using IDFlow value
                var filteredData = table.Select("IDFlow = " + idFlowValue);
                allData.AddRange(filteredData);
            }

            //get total volume for all data
            double totalVolume = table.AsEnumerable().Sum(row => row.Field<double>("Volume"));

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
            chart1.Series.Clear();

            //add series for total volume and each IDFlow volume
            var seriesIDFlow = chart1.Series.Add("IDFlow Volume");
            seriesIDFlow.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Pie;
            seriesIDFlow.BorderWidth = 1;
            seriesIDFlow.IsVisibleInLegend = true;
            seriesIDFlow.LegendText = "#VALX (#PERCENT{P0})";

            //loop through each IDFlow value and add data point to IDFlow volume series
            foreach (int idFlowValue in idFlowValues)
            {
                string seriesName = "FlowMeter" + idFlowValue.ToString();
                double idFlowVolume = idFlowVolumes[idFlowValue];
                double idFlowPercentage = idFlowVolume / totalVolume * 100;
                seriesIDFlow.Points.AddXY(seriesName, idFlowVolume);
                seriesIDFlow.Points.Last().Label = "#PERCENT{P0}";
                seriesIDFlow.Points.Last().ToolTip = seriesName + ": " + idFlowVolume.ToString() + " Liter (" + idFlowPercentage.ToString("F2") + "%)";
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
            if (exportExc)
            {
                exportExc = false;
                ExportToExcel(table, chart1, chart2);
            }
        }
        private void ChartLoger()
        {
            string mode = comboChart.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(mode))
            {
                return;
            }

            chart2.Series.Clear();
            chart2.ChartAreas.Clear();
            chart2.Titles.Clear();

            Series series = new Series
            {
                ChartType = SeriesChartType.Column,
                Name = mode == "Day" ? "Hari" : "Bulan"
            };
            chart2.Titles.Add(mode == "Day" ? "Total Volume Per Hari" : "Total Volume Per Bulan");
            ChartArea area = new ChartArea("Area Chart")
            {
                AxisX =
                {
                    Title = mode == "Day" ? "Hari" : "Bulan",
                    IntervalType = mode == "Day" ? DateTimeIntervalType.Days : DateTimeIntervalType.Months
                },
                AxisY = { Title = "Total Volume Liter" }
            };
            chart2.ChartAreas.Add(area);

            DateTime startDate = dateTimePicker1.Value.Date;
            DateTime endDate = dateTimePicker2.Value.Date.AddDays(1); // Tambah 1 hari agar dapat memfilter data sampai dengan akhir tanggal

            string query = "SELECT DATE_FORMAT(DateTime, '" + (mode == "Day" ? "%Y/%m/%d" : "%Y/%m") + "') AS 'Period', SUM(Volume) AS 'TotalVolume' FROM logdata WHERE DateTime >= @startDate AND DateTime < @endDate GROUP BY Period";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@startDate", startDate);
                    command.Parameters.AddWithValue("@endDate", endDate);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable table1 = new DataTable();
                    adapter.Fill(table1);
                    foreach (DataRow row in table1.Rows)
                    {
                        double volume = Convert.ToDouble(row["TotalVolume"]);
                        string period = row["Period"].ToString();

                        DataPoint point = series.Points.FirstOrDefault(p => p.AxisLabel == period);
                        if (point != null)
                        {
                            point.YValues[0] += volume;
                            point.Label = point.YValues[0].ToString();
                        }
                        else
                        {
                            point = new DataPoint(series.Points.Count + 1, volume)
                            {
                                AxisLabel = period,
                                Label = volume.ToString()
                            };
                            series.Points.Add(point);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
                chart2.ChartAreas[0].Area3DStyle.Enable3D = true;
                chart2.ChartAreas[0].Area3DStyle.IsClustered = false;
                chart2.ChartAreas[0].Area3DStyle.Perspective = 10;
                chart2.ChartAreas[0].Area3DStyle.PointDepth = 100;
            }
            chart2.Legends[0].Enabled = false;
            chart2.Series.Add(series);
            chart2.Invalidate();
            chart2.Refresh();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            exportExc = true;
            ChartLoger();
            updateTabel();
        }

        private void ExportToExcel(DataTable dataTable, Chart chart1, Chart chart2)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Excel Workbook|*.xlsx" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // create workbook and worksheet
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add(dataTable, "logdata");

                            // add chart sheet 1
                            var chartSheet1 = workbook.Worksheets.Add("Chart 1");
                            using (MemoryStream msChart1 = new MemoryStream())
                            {
                                chart1.SaveImage(msChart1, ChartImageFormat.Png);
                                msChart1.Seek(0, SeekOrigin.Begin);

                                var excelImage1 = chartSheet1.AddPicture(msChart1).MoveTo(chartSheet1.Cell(1, 1));
                                excelImage1.Scale(2);
                            }

                            // add chart sheet 2
                            var chartSheet2 = workbook.Worksheets.Add("Chart 2");
                            using (MemoryStream msChart2 = new MemoryStream())
                            {
                                chart2.SaveImage(msChart2, ChartImageFormat.Png);
                                msChart2.Seek(0, SeekOrigin.Begin);

                                var excelImage2 = chartSheet2.AddPicture(msChart2).MoveTo(chartSheet2.Cell(1, 1));
                                excelImage2.Scale(2);
                            }

                            // add total air to worksheet
                            int i = 0;
                            for (i = 0; i < Math.Min(dataTable.Rows.Count, 1048576); i++) { }
                            i += 2;
                            worksheet.Cell(i, 5).Value = "Total";
                            worksheet.Cell(i, 6).Value = Math.Round(total_air, 4);
                            worksheet.Cell(i, 7).Value = "Liter";

                            // save workbook and show success message
                            workbook.SaveAs(sfd.FileName);
                            MessageBox.Show("Data berhasil diekspor ke excel", "INFORMASI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(("exspor data excel :") + ex.Message.ToString(), "EXPORT DATA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

    }
}
