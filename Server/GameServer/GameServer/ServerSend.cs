using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class ServerSend
    {
        private static void SendTCPData(int _toClient, Packet _packet)
        {
            // 패킷앞에 패킷의 길이를 삽입한다.
            _packet.WriteLength();

            // 앞의 함수들을 거쳐서 최종적으로 다음과같은 구조를 이룬다
            // 최종 문자열길이 int 4바이트 / 처음패킷생성할때 등록한 delegate번호 int 4바이트(패킷번호) / 순수문자열길이 int 4바이트 / 문자열 바이트배열 / 클라이언트번호 int 4바이트
            // ServerSendTCP-4 [최종 패킷길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열 / 보낼클라이언트 id int 4바이트]
            // TCP는 서버연결용으로 사용하고 그 후는 UDP로 통신해서 구조가 이런듯하다

            // Client id에 패킷을 보낸다
            Server.clients[_toClient].tcp.SendData(_packet);
        }

        private static void SendUDPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            // 앞의 함수들을 거쳐서 최종적으로 다음과같은 구조를 이룬다
            // 최종 문자열길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열 바이트배열
            // ServerSendUDP-3 [최종 패킷길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
            Server.clients[_toClient].udp.SendData(_packet);
        }

        // 모든클라이언트한테 전송
        private static void SendTCPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }

        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }

        private static void SendUDPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }

        private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }

        #region Packets
        public static void Welcome(int _toClient, string _msg)
        {
            // ServerSendTCP-1 [패킷번호 int 4바이트]
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                // ServerSendTCP-2 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
                _packet.Write(_msg);
                // ServerSendTCP-3 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열 / 보낼클라이언트 id int 4바이트]
                _packet.Write(_toClient);

                // TCP는 서버연결용이기 때문에 구조가 복잡하다
                SendTCPData(_toClient, _packet);
            }
        }

        public static void UDPTest(int _toClient)
        {
            // ServerSendUDP-1 [패킷번호 int 4바이트]
            using (Packet _packet = new Packet((int)ServerPackets.udpTest))
            {
                // ServerSendUDP-2 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
                _packet.Write("A test packet for UDP");

                SendUDPData(_toClient, _packet);
            }
        }
        #endregion
    }
}
