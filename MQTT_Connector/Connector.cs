using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json;


using Lemaju_UDP;

namespace MQTT_Connector
{
    public partial class Connector : Form
    {

        // string BrokerAddress = "lemaju.net";
        string BrokerAddress = "10.10.0.1";
        string attributesTopic = "v1/devices/me/attributes";
        string teleTopic = "v1/devices/me/telemetry";
        string rpcTopic = "v1/devices/me/rpc/request/+";

        List<MqttClient> m_MqttClient_List;
        List<String> m_Rpc_List;
        

        UDPSocket UDP_Server;

        public Connector()
        {
            InitializeComponent();

            Text += " 1.00";

            button_Load_Click(this, null);
        }

        private void button_Load_Click(object sender, EventArgs e)
        {

            m_MqttClient_List = new List<MqttClient>();
            m_Rpc_List = new List<String>();

            //    grid.Columns.Add(i.ToString(), name);



            dataGrid.Columns.Add("0", "ClientName");


            dataGrid.Columns.Add("1", "VPN IP");
            dataGrid.Columns.Add("2", "VPN NET");
            dataGrid.Columns.Add("3", "Routing");

            dataGrid.Columns.Add("4", "Online");
            dataGrid.Columns.Add("5", "TB_AToken");
            dataGrid.Columns.Add("6", "Description");
            dataGrid.Columns.Add("7", "RPC");

            dataGrid.Columns[0].Width = 200;
            // dataGrid.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGrid.Columns[6].Width = 300;
            dataGrid.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var rows = dataGrid.Rows;






            foreach (DataGridViewColumn column in dataGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }




            StreamReader sr = new StreamReader(@"C:\test\LIST.csv");
            // for set encoding
            // StreamReader sr = new StreamReader(@"file.csv", Encoding.GetEncoding(1250));

            string strline = "";
            string[] _values = null;
            int x = 0;
            while (!sr.EndOfStream)
            {

                strline = sr.ReadLine();
                _values = strline.Split(';');

                // if (_values.Length >= 6 && _values[0].Trim().Length > 0)
                {
                    rows.Add(_values[0]);
                    dataGrid.Rows[x].Cells[1].Value = _values[1];
                    dataGrid.Rows[x].Cells[2].Value = _values[2];
                    if (_values.Length > 3) dataGrid.Rows[x].Cells[3].Value = _values[3];
                    if (_values.Length > 5) dataGrid.Rows[x].Cells[5].Value = _values[5];
                    if (_values.Length > 4) dataGrid.Rows[x].Cells[6].Value = _values[4];


                    //  dataGridView1[1, 0].Value = "This is 0,1!"; //Setting the Second cell of the first row!
                    //dataGrid[x,1] = _values[1];
                }
                x++;
            }
            sr.Close();

            int index = 0;
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                string AcessToken = (string)row.Cells[5].Value;
                string NAME = (string)row.Cells[0].Value;
                string _IP = (string)row.Cells[1].Value;

                m_Rpc_List.Add("");

                if (AcessToken.Length > 15)
                {


                    string password = "";
                    string clientId;
                    string username = AcessToken;

                    MqttClient client = new MqttClient(BrokerAddress);
                    client.Last_rpc = "";


                    client.udp_client = new UDPSocket();
                    //   client.udp_client.Client(_IP, 5000); 
                    client.udp_client.Client(_IP, 27001);
                    client.PLC_IP = _IP;
                    //   client.udp_client.sender = client;
                    //  client.udp_client.eventRecive += Udp_client_eventRecive;


                    client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

                    client.ConnectionClosed += Client_ConnectionClosed;

                    // use a unique id as client id, each time we start the application
                    clientId = "LEMAJU_" + NAME + _IP;
                    //    Topic: v1/devices/me/attributes
                    byte code = client.Connect(clientId, username, password);
                    client.L_Index = index;
                    m_MqttClient_List.Add(client);

                    client.Last_rpc = "Connect";

                    Telemet p = new Telemet();
                    p.IP = _IP;
                    p.Client = clientId;
                    p.VPN_NAME = NAME;
                    string json = JsonConvert.SerializeObject(p);
                    client.Publish(attributesTopic, Encoding.UTF8.GetBytes(json));



                    string Topic = "v1/devices/me/attributes";
                    //    "v1/devices/me/attributes"


                    string[] Topics = new string[1];
                    Topics[0] = attributesTopic;

                    byte[] qos = new byte[1];
                    qos[0] = 0;

                    //  client.messageIdCounter = 30000;



                    // subscribe to the attributes Topic topic with QoS 2
                    Topics[0] = attributesTopic;
                    ushort xx = client.Subscribe(Topics, qos);

                    // subscribe to the rpc topic with QoS 2
                    Topics[0] = rpcTopic;
                    xx = client.Subscribe(Topics, qos);
                }
                else m_MqttClient_List.Add(null);

                index++;
            }

            UDP_Server = new UDPSocket();

            UDP_Server.Server("127.0.0.1", 27000);
            UDP_Server.eventRecive += Udp_Server_eventRecive;
            //  client.udp_client.eventRecive += Udp_client_eventRecive;

            timer1.Enabled = true;
            timer_Refresh.Enabled = true;
        }

        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            MqttClient client = (MqttClient)sender;
            client.Last_rpc = " ! ConnectionClosed";
        }

        private void Udp_Server_eventRecive(object sender, string msg)
        {
            string IP = (string)sender;
            foreach (MqttClient client in m_MqttClient_List)
            {
                if (client != null)
                {
                    if (client.PLC_IP == IP)
                    {
                        //client.Publish(teleTopic, Encoding.UTF8.GetBytes(msg));
                        client.Last_rpc = msg;

                        Telemet p = new Telemet();
                        p.IP = "I123";
                        p.Client = "I124";
                        p.VPN_NAME = "I125";
                        string json = JsonConvert.SerializeObject(p);
                         json = msg;
                        client.Publish(teleTopic, Encoding.UTF8.GetBytes(json));

                        client.Last_rpc = json;

                    }
                }
            }
       
          

            ;
        

        }

        private void Connector_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Enabled = false;
            foreach (MqttClient client in m_MqttClient_List)
            {
                if (client != null)
                    client.Disconnect();



            }

            UDP_Server.Close();


            Thread.Sleep(500);
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            MqttClient client = (MqttClient)sender;
            client.RPC_CNT();
            client.Last_rpc = ReceivedMessage;
            client.udp_client.Send(ReceivedMessage);
          //  m_Rpc_List[i];
        }

        Setup setup=null;
        private void dataGrid_DoubleClick(object sender, EventArgs e)
        {
            if (setup != null)
            {
                setup.to_save = false;
                setup.Dispose();
                }

             setup = new Setup();
           // setup.TopLevel = false;
           // setup.Parent = this;
            setup.Show();

            int row = dataGrid.CurrentCell.RowIndex;
            setup.row_i = row;

            // dataGrid.Rows[row].Cells[3].Value =

            setup.textBox_name.Text = (string)(dataGrid.Rows[row].Cells[0].Value);
            setup.textBox_ip.Text = (string)(dataGrid.Rows[row].Cells[1].Value);
            setup.textBox_nat.Text = (string)(dataGrid.Rows[row].Cells[2].Value);
            setup.textBox_route.Text = (string)(dataGrid.Rows[row].Cells[3].Value);
            // Online State 4
            setup.textBox_AT.Text = (string)(dataGrid.Rows[row].Cells[5].Value);

            setup.textBox_desc.Text = (string)(dataGrid.Rows[row].Cells[6].Value);

            setup.FormClosing += Setup_FormClosing;

        }

        private void Setup_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (setup.to_save)
            {

                dataGrid.Rows[setup.row_i].Cells[0].Value= setup.textBox_name.Text ;
                dataGrid.Rows[setup.row_i].Cells[1].Value=setup.textBox_ip.Text ;
                dataGrid.Rows[setup.row_i].Cells[2].Value=setup.textBox_nat.Text ;
                dataGrid.Rows[setup.row_i].Cells[3].Value=setup.textBox_route.Text  ;

                dataGrid.Rows[setup.row_i].Cells[5].Value = setup.textBox_AT.Text ;      
                dataGrid.Rows[setup.row_i].Cells[6].Value = setup.textBox_desc.Text;

            }

            setup = null;
        }

  
        private void button1_Click_Save(object sender, EventArgs e)
        {
            using (var w = new StreamWriter(@"C:\test\LIST.csv"))
            {
                foreach (DataGridViewRow row in dataGrid.Rows)
                {
                    string first = (string)row.Cells[0].Value;
                    string second = (string)row.Cells[1].Value;
                    string third = (string)row.Cells[2].Value;
                    string quart = (string)row.Cells[3].Value;
                 //   row.Cells[4].Value;  online state
                    
                    string quint = (string)row.Cells[5].Value;
                    string sixt = (string)row.Cells[6].Value;

                    var line = string.Format("{0};{1};{2};{3};{4};{5}", first, second, third, quart,  sixt, quint);
                    w.WriteLine(line);
                    w.Flush();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
      
        
        }

        private void Pinge(string who, string Referenz)
        {

       
            Ping pingSender = new Ping();

            // When the PingCompleted event is raised,
            // the PingCompletedCallback method is called.
            pingSender.PingCompleted += PingSender_PingCompleted;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 12 seconds for a reply.
            int timeout = 2000;

            // Set options for transmission:
            // The data can go through 64 gateways or routers
            // before it is destroyed, and the data packet
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);


            // Send the ping asynchronously.
            // Use the waiter as the user token.
            // When the callback completes, it can wake up this thread.
            if (who.Length>4)
            pingSender.SendAsync(who, timeout, buffer, options, Referenz);

            // Prevent this example application from ending.
            // A real application should do something useful
            // when possible.

        }

        private void PingSender_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (!timer1.Enabled) return;
            int row = int.Parse((string)e.UserState);
            if (e.Reply != null)
            {

                if (e.Reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    dataGrid.Rows[row].Cells[4].Value = e.Reply.RoundtripTime.ToString() + "ms";
                    dataGrid.Rows[row].Cells[4].Style.BackColor = Color.Lime;

                   // Color.White

                }

                else
                {
                    dataGrid.Rows[row].Cells[4].Value = "offline";
                    dataGrid.Rows[row].Cells[4].Style.BackColor = Color.WhiteSmoke;

                }
                //   throw new NotImplementedException();
            }
        

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int x = 0;
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                string first = (string)row.Cells[0].Value;
                string second = (string)row.Cells[1].Value;
                Pinge(second, x.ToString());
                     x++;
            }
        }

        private void timer_Refresh_Tick(object sender, EventArgs e)
        {
            int i = 0;
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (m_MqttClient_List[i]!=null)
                row.Cells[7].Value = m_MqttClient_List[i].Get_Info_RPC();

                i++;
            }
               
        }

        private void timer_Keep_Alive_Tick(object sender, EventArgs e)
        {
            foreach (MqttClient client in m_MqttClient_List)
            {
                if (client != null)
                {
                    client.Last_rpc = "X";
                    client.udp_client.Send("X");

                }
            }
        }
    }
    public class Telemet
    {
        public string IP { get; set; }
        public string Client { get; set; }
        public string VPN_NAME { get; set; }
    }


}
