using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace FlowMeterFactory
{
    public class DataDB
    {
        public static void InsertData(int IDFlow, string FlowMeter, double FlowRate, double SetVolume, double Volume, string Mode, string ToTank, string FromSource, DateTime DateTime)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost; uid=root; database=logflowmeter");
            try
            {
                conn.Open();
                string sql = "INSERT INTO logdata(IDFlow, FlowMeter, FlowRate, SetVolume, Volume, Mode, ToTank, FromSource, DateTime) VALUES(@IDFlow, @FlowMeter, @FlowRate, @SetVolume, @Volume, @Mode, @ToTank, @FromSource, @DateTime)";

                // membuat objek command untuk eksekusi query SQL
                MySqlCommand command = new MySqlCommand(sql, conn);

                // parameter untuk query SQL
                command.Parameters.AddWithValue("@IDFlow", IDFlow);
                command.Parameters.AddWithValue("@FlowMeter", FlowMeter);
                command.Parameters.AddWithValue("@FlowRate", FlowRate);
                command.Parameters.AddWithValue("@SetVolume", SetVolume);
                command.Parameters.AddWithValue("@Volume", Volume);
                command.Parameters.AddWithValue("@Mode", Mode);
                command.Parameters.AddWithValue("@ToTank", ToTank);
                command.Parameters.AddWithValue("@FromSource", FromSource);
                command.Parameters.AddWithValue("@DateTime", DateTime);

                // mengeksekusi query SQL
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    //MessageBox.Show("Data berhasil ditambahkan.");
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Gagal menambahkan data ke tabel logdata: " + ex.Message);
            }
            finally
            {
                // menutup koneksi
                conn.Close();
            }
        }
    }
}
