using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Lemaju_UDP
{

    public delegate void EventRecive(string msg);
    public class UDPSocket
    {
        private Socket serverSocket = null;
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        public object sender;

        private byte[] byteData = new byte[1024];


        public event EventRecive eventRecive;
        public delegate void EventRecive(object sender, string msg);

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(string address, int port)
        {
            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            this.serverSocket.BeginReceiveFrom(this.byteData, 0, this.byteData.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, newClientEP);

          //  Receive();
        }

        private void DoReceiveFrom(IAsyncResult iar)
        {
            try
            {
                EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
                int dataLen = this.serverSocket.EndReceiveFrom(iar, ref clientEP);
                byte[] data = new byte[dataLen];
                Array.Copy(this.byteData, data, dataLen);
                string msg = System.Text.Encoding.UTF8.GetString(data);

                // if (!this.clientList.Any(client => client.Equals(clientEP)))
                //     this.clientList.Add(clientEP);
                clientEP.AddressFamily.ToString();

                string IP = clientEP.ToString().Split(':')[0];
                eventRecive(IP, msg);

                EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                this.serverSocket.BeginReceiveFrom(this.byteData, 0, this.byteData.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, newClientEP);

               
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Client(string address, int port)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            Receive();
        }

        public void Close()
        {
            _socket.Close();
   
        }


        public void Send(string text)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(text);
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
                {
                    State so = (State)ar.AsyncState;
                    int bytes = _socket.EndSend(ar);

                }, state);
            }
            catch { }
        }

        private void Receive()
        {
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                // Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));

                if (eventRecive != null)
                {
                    eventRecive(sender,"Process() begin");
                }
            }, state);
        }
    }
}
