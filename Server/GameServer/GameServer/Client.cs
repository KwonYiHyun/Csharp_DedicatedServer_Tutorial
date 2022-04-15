using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    class Client
    {
        // 버퍼 사이즈 설정
        public static int dataBufferSize = 4096;
        public int id;
        public TCP tcp;
        public UDP udp;

        public Client(int _cliendId)
        {
            // 클라이언트 ID설정
            id = _cliendId;
            // TCP통신에 사용할 객체들을 담고있는 TCP클래스 생성
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
            // 받은 데이터
            private Packet receivedData;
            // 전송할 데이터
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                // 매개변수의 TcpClient로 socket등록
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();

                receiveBuffer = new byte[dataBufferSize];

                // BeginRead()
                // 비동기로 스트림읽기를 시작한다
                // 데이터를받을 바이트버퍼 / 버퍼에서 데이터 저장을 시작할 위치 / 읽을 바이트 크기 / 콜백
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReciveCallback, null);

                // ServerSend.Welcome() -> ServerSend.SendTCPData() -> Client.SendData()
                ServerSend.Welcome(id, "Welcome to the server!");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    // 소켓이 비어있지 않는다면
                    if (socket != null)
                    {
                        // BeginWrite()
                        // 스트림에 대한 비동기 쓰기를 시작합니다.
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch(Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }

            // 스트림에 데이터가 왔을 떄 콜백동작
            // 클라이언트측에서 패킷이 왔을 때의 구조
            // ReceiveTCP-1 [최종 패킷 길이 int 4바이트 / 패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
            private void ReciveCallback(IAsyncResult _result)
            {
                try
                {
                    // EndRead()
                    // 비동기 읽기 끝을 처리 시스템에서 읽은 바이트수를 반환한다
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        return;
                    }

                    // 스트림에서 읽은 바이트데이터
                    byte[] _data = new byte[_byteLength];
                    // 배열의 요소 범위를 캐스팅한다
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data));

                    // BeginRead()
                    // 비동기로 스트림읽기를 시작한다
                    // 데이터를받을 바이트버퍼 / 버퍼에서 데이터 저장을 시작할 위치 / 읽을 바이트 크기 / 콜백
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReciveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLenght = 0;

                // Packet 클래스의 buffer에 등록
                receivedData.SetBytes(_data);

                // 읽지않은 길이가 4이상이면
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLenght = receivedData.ReadInt();
                    // ReceiveTCP-2 [패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열] (패킷길이 읽음)
                    if (_packetLenght <= 0)
                    {
                        return true;
                    }
                }

                // 패킷의 길이가 0이상 이면서 읽지않은 길이보다 작을 때
                while (_packetLenght > 0 && _packetLenght <= receivedData.UnreadLength())
                {
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLenght);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            // ReceiveTCP-3 [내 클라이언트 id int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열] (패킷번호 읽음)
                            Server.packetHandlers[_packetId](id, _packet);
                        }
                    });

                    _packetLenght = 0;

                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLenght = receivedData.ReadInt();
                        if (_packetLenght <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (_packetLenght <= 1)
                {
                    return true;
                }

                return false;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;
            private int id;
            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
                ServerSend.UDPTest(id);
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                // ReceiveUDP-3 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열] (패킷길이 읽음)
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using(Packet _packet=new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        // ReceiveUDP-4 [문자열길이 int 4바이트 / 문자열 바이트배열] (패킷번호 읽음)
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });
            }
        }
    }
}
