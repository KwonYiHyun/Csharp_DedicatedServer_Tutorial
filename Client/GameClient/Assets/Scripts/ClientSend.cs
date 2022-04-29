using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameServer;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using System.Runtime.Serialization;
using System.Xml.Linq;

public class ClientSend : MonoBehaviour
{
    private static void SendTCPData(Packet _packet){
        _packet.WriteLength();
        // ClientSendTCP-4 [최종 패킷 길이 int 4바이트 / 패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
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
            // ClientSendTCP-3 [패킷번호 int 4바이트 / 내 클라이언트 id int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
            SendTCPData(_packet);
        }
    }

    // public static void UDPTestReceived(){
    //     using(Packet _packet=new Packet((int)ClientPackets.udpTestReceive)){
    //         // ClientSendUDP-1 [패킷번호 int 4바이트]
    //         _packet.Write("Received a UDP packet.");
    //         // ClientSendUDP-2 [패킷번호 int 4바이트 / 문자열길이 int 4바이트 / 문자열 바이트배열]
    //         SendUDPData(_packet);
    //     }
    // }

    public static void PlayerMovement(bool[] _inputs){
        using (Packet _packet=new Packet((int)ClientPackets.playerMovement)){
            _packet.Write(_inputs.Length);
            foreach(bool _input in _inputs){
                _packet.Write(_input);
            }
            _packet.Write(GameManager.players[Client.instance.myId].transform.rotation);

            SendUDPData(_packet);
        }
    }

    public static void PlayerShoot(Vector3 _facing){
        using(Packet _packet=new Packet((int)ClientPackets.playerShoot)){
            _packet.Write(_facing);

            SendTCPData(_packet);
        }
    }

    public static void unityChan(){
        using(Packet _packet=new Packet((int)ClientPackets.unityChan)){
            string path = @"D:\git\Csharp_DedicatedServer_Tutorial\Client\GameClient\Assets\unity-chan!\Unity-chan! Model\Art\Models\unitychan.fbx";

            byte[] bytes = FileToByteArray(path);
            Debug.Log("len = " + bytes.Length);
            _packet.Write(bytes.Length);
            _packet.Write(bytes);

            SendTCPData(_packet);
        }
    }
    #endregion

    public static byte[] FileToByteArray(string path){
        byte[] fileBytes = null;
        try
        {
            using(FileStream fileStream=new FileStream(path, FileMode.Open)){
                fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        return fileBytes;
    }

    public static bool ByteArrayToFile(string path, byte[] buffer){
        try
        {
            File.WriteAllBytes(path, buffer);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        return false;
    }
}
