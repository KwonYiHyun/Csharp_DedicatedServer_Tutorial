using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        // 클라이언트를 담고있는 배열
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static void Start(int _maxPlayer, int _port)
        {
            // 서버 최대명수와 포트설정
            MaxPlayers = _maxPlayer;
            Port = _port;

            Console.WriteLine("Starting server...");
            InitializeServerData();

            // TCP 설정
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            // 비동기로 TCP연결을 받고 콜백을 설정
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

            // UDP 설정
            udpListener = new UdpClient(Port);
            // 비동기로 UDP연결을 받고 콜백을 설정
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on {Port}");
        }

        private static void TCPConnectCallback(IAsyncResult _result)
        {
            // 연결되는 시도를 비동기적으로 수락하고 새 시스템을 만든다
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i < MaxPlayers; i++)
            {
                // 생성은 되어있지만 등록은 안되있는 클라일 경우
                if (clients[i].tcp.socket == null)
                {
                    // 연결한 TcpClient로 Client객체를 만들어 등록시킨다
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        // 클라이언트측에서 패킷이 왔을 때의 구조
        // ReceiveUDP-1 [이패킷을보낸 클라이언트 id int 4바이트 / 최종 패킷 길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet=new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();
                    // ReceiveUDP-2 [최종 패킷 길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열] (클라이언트 id읽음)

                    if (_clientId == 0)
                    {
                        return;
                    }

                    // endPoint(끝점)이 null이면 udp첫 연결이기 때문에 Connect호출
                    if (clients[_clientId].udp.endPoint == null)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    // 이미 연결된 udp통신에서 패킷을 받았을 경우 HandleData를 통해서 정해진 delegate에 등록해둔 함수 호출을 위해서 HandleData호출
                    // 분해 순서로 봤을 때 그럼 처음 udp패킷 모양은
                    // 클라이언트 id int 4바이트 / 패킷길이 int 4바이트 / 문자열 바이트배열 / 패킷번호 int 4바이트
                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch(Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch(Exception _ex)
            {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        private static void InitializeServerData()
        {
            // 클라이언트객체를 미리 최대 플레이어만큼 생성하여 배열에 등록함
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                //{ (int)ClientPackets.udpTestReceive, ServerHandle.UDPTestReceived }
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
            };
            Console.WriteLine("Initialized packets.");
        }
    }
}
