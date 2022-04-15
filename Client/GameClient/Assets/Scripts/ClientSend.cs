using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameServer;

public class ClientSend : MonoBehaviour
{
    private static void SendTCPData(Packet _packet){
        _packet.WriteLength();
        // ClientSendTCP-4 [최종 패킷 길이 int 4바이트 / 패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트 / 닉네임 문자열 바이트배열]
        Client.instance.tcp.SendData(_packet);
    }

    private static void SendUDPData(Packet _packet){
        _packet.WriteLength();
        // ClientSendUDP-3 [최종 패킷 길이 int 4바이트 / 패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
        Client.instance.udp.SendData(_packet);
    }

    #region Packets
    public static void WelcomeReceived(){
        using(Packet _packet=new Packet((int)ClientPackets.welcomeReceived)){
            // ClientSendTCP-1 [패킷번호 int 4바이트]
            _packet.Write(Client.instance.myId);
            // ClientSendTCP-2 [패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트]
            _packet.Write(UIManager.instance.usernameField.text);
            // ClientSendTCP-3 [패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트 / 닉네임 문자열 바이트배열]
            SendTCPData(_packet);
        }
    }

    public static void UDPTestReceived(){
        using(Packet _packet=new Packet((int)ClientPackets.udpTestReceive)){
            // ClientSendUDP-1 [패킷번호 int 4바이트]
            _packet.Write("Received a UDP packet.");
            // ClientSendUDP-2 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
            SendUDPData(_packet);
        }
    }
    #endregion
}
