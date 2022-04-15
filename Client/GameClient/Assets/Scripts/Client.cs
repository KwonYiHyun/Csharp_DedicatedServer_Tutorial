using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using GameServer;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize=4096;

    public string ip="127.0.0.1";
    public int port=26950;
    public int myId=0;
    public TCP tcp;
    public UDP udp;
    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake() {
        if(instance==null){
            instance=this;
        }else if(instance!=this){
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start() {
        tcp=new TCP();
        udp = new UDP();
    }

    public void ConnectToServer(){
        InitializeClientData();
        tcp.Connect();
    }

    public class UDP{
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP(){
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int _localPort){
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using(Packet _packet=new Packet()){
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet){
            try{
                _packet.InsertInt(instance.myId);
                if(socket!=null){
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }catch(Exception _ex){
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        // 서버측에서 패킷이 왔을 때의 구조
        // ReceiveUDP-1 [최종 패킷길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
        private void ReceiveCallback(IAsyncResult _result){
            try{
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if(_data.Length<4){
                    return;
                }

                HandleData(_data);
            }catch{

            }
        }

        private void HandleData(byte[] _data){
            using(Packet _packet=new Packet(_data)){
                int _packetLenght = _packet.ReadInt();
                // ReceiveUDP-2 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
                _data = _packet.ReadBytes(_packetLenght);
            }

            ThreadManager.ExecuteOnMainThread(() => { 
                using(Packet _packet=new Packet(_data)){
                    int _packetId = _packet.ReadInt();
                    // ReceiveUDP-3 [문자열길이 int 4바이트 / 문자열 바이트배열]
                    packetHandlers[_packetId](_packet);
                }
            });
        }
    }

    public class TCP{
        public TcpClient socket;
        private Packet receivedData;
        private NetworkStream stream;
        private byte[] receiveBuffer;

        public void Connect(){
            socket=new TcpClient{
                ReceiveBufferSize=dataBufferSize,
                SendBufferSize=dataBufferSize
            };

            receiveBuffer=new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }
        private void ConnectCallback(IAsyncResult _result){
            socket.EndConnect(_result);
            
            if(!socket.Connected){
                return;
            }

            stream=socket.GetStream();

            receivedData=new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet _packet){
            try{
                if(socket!=null){
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }catch(Exception _ex){
                Debug.Log($"Error sending data to server via Tcp: {_ex}");
            }
        }

        // 서버측에서 패킷이 왔을 때의 구조
        // ReceiveTCP-1 [최종 패킷길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열 / 이패킷을보낸 클라이언트 id int 4바이트]
        private void ReceiveCallback(IAsyncResult _result){
            try
            {
                int _byteLength=stream.EndRead(_result);
                if(_byteLength<=0){
                    return;
                }

                byte[] _data=new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                
            }
        }

        private bool HandleData(byte[] _data){
            int _packetLenght = 0;

            receivedData.SetBytes(_data);

            if(receivedData.UnreadLength()>=4){
                _packetLenght = receivedData.ReadInt();
                // ReceiveTCP-2 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열 / 보낼클라이언트 id int 4바이트]
                if(_packetLenght<=0){
                    return true;
                }
            }

            while (_packetLenght>0 && _packetLenght <= receivedData.UnreadLength())
            {
                byte[] _packetBytes = receivedData.ReadBytes(_packetLenght);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet=new Packet(_packetBytes)){
                        int _packetId = _packet.ReadInt();
                        // ReceiveTCP-3 [문자열길이 int 4바이트 / 문자열 바이트배열 / 보낼클라이언트 id int 4바이트]
                        packetHandlers[_packetId](_packet);
                    }
                });

                _packetLenght = 0;

                if(receivedData.UnreadLength()>=4){
                    _packetLenght = receivedData.ReadInt();
                    if(_packetLenght<=0){
                        return true;
                    }
                }
            }

            if(_packetLenght<=1){
                return true;
            }

            return false;
        }
    }

    private void InitializeClientData(){
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.udpTest, ClientHandle.UDPTest }
        };
        Debug.Log("Initialized packets.");
    }

}
