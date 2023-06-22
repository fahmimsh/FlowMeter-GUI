using System;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SymbolFactoryDotNet;
using Newtonsoft.Json;
using MQTTnet;
using MQTTnet.Client;

namespace FlowMeterFactory
{
    public partial class Form1 : Form
    {
        private CutawayControl[] levelControl;
        private StandardControl[] pipeInFl1, pipeInFl2, pipeInFl3, pipeInFl4;
        private Button[] btnOnOff, btnSrc, btnTank;
        private TextBox[] textBoxTank, texBoxSrc, textBoxAutoManual;
        private NumericUpDown[] numericSetLiter, numericLiter, numericLpm;
        private int[] idMqtt, Tank, SourceTank, TankPrev, SourceTankPrev;
        private string HostMqtt;
        public static String[] tagTangk, tagSource, tagFlowmeter, tagMode;
        private double[] Liter, Lpm, Sltr, LiterPrev, LpmPrev, SltrPrev;
        private bool[] OnOffdev, AutoManual, OnOffdevPrev, AutoManualPrev;
        
        public Form1()
        {
            InitializeComponent();
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress localIP in localIPs)
            {
                if (localIP.AddressFamily == AddressFamily.InterNetwork)
                {
                    HostMqtt = localIP.ToString();
                    toolStripStatusLabel1.Text = $"ip:{localIP}:MQTT";
                    break;
                }else { HostMqtt = "127.0.0.1"; }
            }
            
            levelControl = new CutawayControl[] { LevelControl1, LevelControl2, LevelControl3, LevelControl4 };
            pipeInFl1 = new StandardControl[] {pipeInA1, pipeInA2, pipeInA3, pipeInA4, pipeInA5, pipeInA6, pipeInA7, pipeInA8, pipeInA9, pipeInA10, pipeInA11};
            pipeInFl2 = new StandardControl[] {pipeInB1, pipeInB2, pipeInB3, pipeInB4, pipeInB5, pipeInB6, pipeInB7, pipeInB8, pipeInB9, pipeInB10, pipeInB11, pipeInB12, pipeInB13, pipeInB14, pipeInB15, pipeInB16, pipeInB17};
            pipeInFl3 = new StandardControl[] {pipeInC1};
            pipeInFl4 = new StandardControl[] {pipeInD1};
            btnOnOff = new Button[] { btnOnOff1, btnOnOff2, btnOnOff3, btnOnOff4 };
            btnSrc = new Button[] { btnSrc1, btnSrc2, btnSrc3, btnSrc4 };
            btnTank = new Button[] { btnTank1, btnTank2, btnTank3, btnTank4 };
            textBoxTank = new TextBox[] { textBoxTank1, textBoxTank2, textBoxTank3, textBoxTank4 };
            texBoxSrc = new TextBox[] { textBoxSrc1, textBoxSrc2, textBoxSrc3, textBoxSrc4 };
            textBoxAutoManual = new TextBox[] { textBoxAutoManual1, textBoxAutoManual2, textBoxAutoManual3, textBoxAutoManual4 };
            numericSetLiter = new NumericUpDown[] { numericSetLiter1, numericSetLiter2, numericSetLiter3, numericSetLiter4 };
            numericLiter = new NumericUpDown[] { numericLiter1, numericLiter2, numericLiter3, numericLiter4 };
            numericLpm = new NumericUpDown[] { numericLpm1, numericLpm2, numericLpm3, numericLpm4 };
            idMqtt = new int[] { 1, 2, 3, 4 };
            tagTangk = new string[] { "Tank 1 DBjaket", "Tank 2 DBjaket", "Tank 3 DBjaket", "Tank 4 DBjaket" };
            tagSource = new string[] { "Sumber RO", "Sumber Air" };
            tagFlowmeter = new string[] { "FlowMeter1", "FlowMeter2", "FlowMeter3", "FlowMeter4" };
            int indexId = idMqtt.Length;
            tagMode = new string[indexId];
            Tank = new int[indexId];  SourceTank = new int[indexId]; TankPrev = new int[indexId]; SourceTankPrev = new int[indexId];
            Liter = new double[indexId]; Lpm = new double[indexId]; Sltr = new double[indexId]; LiterPrev = new double[indexId]; LpmPrev = new double[indexId]; SltrPrev = new double[indexId];
            OnOffdev = new bool[indexId]; AutoManual = new bool[indexId]; OnOffdevPrev = new bool[indexId]; AutoManualPrev = new bool[indexId];
            for (int i=0; i<btnOnOff.Length; i++)
            {
                int index = i;
                btnOnOff[i].Click += (s, e) =>
                {
                    JsonParseSetOnOff dataSend = new JsonParseSetOnOff{onf = true,};
                    sendMqtt(HostMqtt, $"fl/{idMqtt[index]}/set", JsonConvert.SerializeObject(dataSend));
                };
                btnTank[i].Click += (s, e) =>
                {
                    JsonParseTankSW dataSend = new JsonParseTankSW { tank = PopUp.SetTag(Tank[index], tagTangk), };
                    sendMqtt(HostMqtt, $"fl/{idMqtt[index]}/tank", JsonConvert.SerializeObject(dataSend));
                };
                btnSrc[i].Click += (s, e) =>
                {
                    JsonParseSrcSW dataSend = new JsonParseSrcSW { src = PopUp.SetTag(SourceTank[index], tagSource), };
                    sendMqtt(HostMqtt, $"fl/{idMqtt[index]}/src", JsonConvert.SerializeObject(dataSend));
                };
                numericSetLiter[i].Click += (s, e) =>
                {
                    double setLiter = Sltr[index];
                    setLiter = PopUp.SetLiter(setLiter);
                    Sltr[index] = setLiter != -1 ? setLiter : Sltr[index];
                    JsonParseSltr dataSend = new JsonParseSltr { sltr = setLiter, };
                    sendMqtt(HostMqtt, $"fl/{idMqtt[index]}/val", JsonConvert.SerializeObject(dataSend));
                };
                textBoxTank[index].Text = tagTangk[Tank[index]];
                texBoxSrc[index].Text = tagSource[SourceTank[index]];
            }
            SettingToolStripMenu.Click += (s, e) =>
            {
                Form FormSetting = new Setting();
                FormSetting.Show();
            };
            dataLoggerToolStripMenuItem.Click += (s, e) =>
            {
                Form form2 = new Form2();
                form2.Show();
            };
            helpToolStripMenuItem.Click += (s, e) =>
            {

            };
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            using (Form form = new Form())
            {
                form.StartPosition = FormStartPosition.CenterScreen;
            }
            await MqttConnectionStart(HostMqtt, 1883);
        }
        private async Task MqttConnectionStart(string localIPs, int Port)
        {
            var mqttFactory = new MqttFactory();
            IMqttClient client = mqttFactory.CreateMqttClient();
            var tlsOptions = new MqttClientTlsOptions{ UseTls = false, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true, AllowUntrustedCertificates = true};
            var options = new MqttClientOptionsBuilder().WithClientId("FlowMeter").WithTcpServer(localIPs, Port).WithKeepAlivePeriod(TimeSpan.FromSeconds(5)).WithCleanSession(true).Build();
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder();
            foreach (var topic in idMqtt)
            {
                mqttSubscribeOptions.WithTopicFilter(f => f.WithTopic($"fl/{topic}/status"));
                mqttSubscribeOptions.WithTopicFilter(f => f.WithTopic($"fl/{topic}/log"));
            }
            var optionsSubs = mqttSubscribeOptions.Build();
            try
            {
                client.ConnectedAsync += OnSubscriberConnected;
                client.DisconnectedAsync += OnSubscriberDisconnected;
                client.ApplicationMessageReceivedAsync += this.OnSubscriberMessageReceived;
                await client.ConnectAsync(options);
                await client.SubscribeAsync(optionsSubs);
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = "MQTT Disconnected" + ex.Message.ToString();
                await client.DisconnectAsync();
            }
        }
        private Task OnSubscriberConnected(MqttClientConnectedEventArgs _)
        {
            toolStripStatusLabel1.Text = "MQTT Connected"; return Task.CompletedTask;
        }
        private async Task OnSubscriberDisconnected(MqttClientDisconnectedEventArgs _)
        {
            toolStripStatusLabel1.Text = "MQTT Connecting"; await Task.Delay(TimeSpan.FromSeconds(5)); await MqttConnectionStart(HostMqtt, 1883);
        }
        string payload = string.Empty; int idTopic = 0;

        public static string[] TagFlowmeter { get; internal set; }

        private Task OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            payload = x.ApplicationMessage.ConvertPayloadToString();
            var topic = x.ApplicationMessage.Topic; string[] topics = topic.Split('/');
            if (topics.Length < 3){return Task.CompletedTask;}
            idTopic = Convert.ToInt32(topics[1]) - 1;
            try
            {
                switch (topics[2])
                {
                    case "status":
                        Invoke(new EventHandler(TerimadataMqttstatus));
                        break;
                    case "log":
                        Invoke(new EventHandler(TerimadataMqttLog));
                        break;
                    default:
                        return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = $"Parse Data: {ex.Message}";
            }
            return Task.CompletedTask;
        }
        private void TerimadataMqttstatus(object sender, EventArgs e)
        {
            if (idTopic < idMqtt.Length)
            {
                int id = idTopic;
                try
                {
                    JsonParseStatusOrLog mqttStatus = JsonConvert.DeserializeObject<JsonParseStatusOrLog>(payload);
                    Liter[id] = Math.Round(mqttStatus.ltr, 1);
                    Lpm[id] = Math.Round(mqttStatus.lpm, 1);
                    Sltr[id] = Math.Round(mqttStatus.sltr, 1);
                    OnOffdev[id] = mqttStatus.onf;
                    AutoManual[id] = mqttStatus.am;
                    Tank[id] = mqttStatus.tank;
                    SourceTank[id] = mqttStatus.src;
                }
                catch (Exception ex)
                {
                    toolStripStatusLabel1.Text = $"JsonData: {ex.Message}";
                }
                if (OnOffdev[id] != OnOffdevPrev[id])
                {
                    OnOffdevPrev[id] = OnOffdev[id];
                    btnOnOff[id].BackColor = OnOffdev[id] ? Color.FromArgb(192, 0, 0) : Color.FromArgb(0, 192, 0);
                    btnOnOff[id].Text = OnOffdev[id] ? "OFF" : "ON";
                    pipeIdiscrete(id, OnOffdev[id]);
                }

                if (AutoManual[id] != AutoManualPrev[id])
                {
                    AutoManualPrev[id] = AutoManual[id];
                    tagMode[id] = AutoManual[id] ? "Auto" : "Manual";
                    textBoxAutoManual[id].Text = tagMode[id];
                }

                if (Tank[id] != TankPrev[id] && Tank[id] < tagTangk.Length)
                {
                    TankPrev[id] = Tank[id];
                    textBoxTank[id].Text = tagTangk[Tank[id]];
                }

                if (SourceTank[id] != SourceTankPrev[id] && SourceTank[id] < tagSource.Length)
                {
                    SourceTankPrev[id] = SourceTank[id];
                    texBoxSrc[id].Text = tagSource[SourceTank[id]];
                }

                if (Sltr[id] != SltrPrev[id])
                {
                    SltrPrev[id] = Sltr[id];
                    numericSetLiter[id].Value = (decimal)Sltr[id];
                }

                if (Lpm[id] != LpmPrev[id])
                {
                    LpmPrev[id] = Lpm[id];
                    numericLpm[id].Value = (decimal)Lpm[id];
                }

                if (Liter[id] != LiterPrev[id])
                {
                    LiterPrev[id] = Liter[id];
                    numericLiter[id].Value = (decimal)Liter[id];
                    levelControl[id].Level = (Liter[id] > Sltr[id]) ? 100 : (double)Liter[id] / Sltr[id] * 100;
                }
            }
            //listBox1.Invoke((MethodInvoker)(() => listBox1.Items.Add(payload + " Status")));
        }
        private void TerimadataMqttLog(object sender, EventArgs e)
        {
            if (idTopic < idMqtt.Length)
            {
                int id = idTopic;
                try
                {
                    JsonParseStatusOrLog mqttLoq = JsonConvert.DeserializeObject<JsonParseStatusOrLog>(payload);
                    Liter[id] = Math.Round(mqttLoq.ltr, 1);
                    Lpm[id] = Math.Round(mqttLoq.lpm, 1);
                    Sltr[id] = Math.Round(mqttLoq.sltr, 1);
                    OnOffdev[id] = mqttLoq.onf;
                    AutoManual[id] = mqttLoq.am;
                    Tank[id] = mqttLoq.tank;
                    SourceTank[id] = mqttLoq.src;
                }
                catch (Exception ex)
                {
                    toolStripStatusLabel1.Text = $"Log: {ex.Message}";
                }
                tagMode[id] = AutoManual[id] ? "Auto" : "Manual";
                DataDB.InsertData(idMqtt[id], tagFlowmeter[id], Lpm[id], Sltr[id], Liter[id], tagMode[id], tagTangk[Tank[id]], tagSource[SourceTank[id]], DateTime.Now);
            }
        }
        private void sendMqtt(string HostMqtt, string topic, string payload)
        {
            Task.Run(async () =>
            {
                var mqttFactory = new MqttFactory();
                using (var mqttClient = mqttFactory.CreateMqttClient())
                {
                    var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(HostMqtt).Build();
                    await mqttClient.ConnectAsync(mqttClientOptions, System.Threading.CancellationToken.None);
                    var applicationMessage = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(Encoding.UTF8.GetBytes(payload)).Build();
                    await mqttClient.PublishAsync(applicationMessage, System.Threading.CancellationToken.None);
                    await mqttClient.DisconnectAsync();
                }
            });
        }
        private void pipeIdiscrete(int pipeIndex, bool status)
        {
            var pipes = new[] { pipeInFl1, pipeInFl2, pipeInFl3, pipeInFl4 };
            if (pipeIndex < 0 || pipeIndex >= pipes.Length)
            {
                throw new ArgumentException($"Invalid pipe index: {pipeIndex}. Please use a value between 0-3.");
            }
            foreach (var control in pipes[pipeIndex])
            {
                control.DiscreteValue2 = status;
            }
        }
    }
}
