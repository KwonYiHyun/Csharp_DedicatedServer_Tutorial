﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameServer;
using System.Net;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet _packet){
        // ReadString()
        // ReadInt를 통해서 문자열의 길이를 읽고 그 후 읽은 길이만큼 문자열을 읽어서 리턴한다
        string _msg=_packet.ReadString();
        // ReceiveTCP-4 [보낼클라이언트 id int 4바이트] (문자열길이, 문자열 읽음)

        int _myId=_packet.ReadInt();
        // ReceiveTCP-5 [] (클라이언트 id 읽음)

        Debug.Log($"Message from server: {_msg}");
        // 내 클라이언트 id 배정받음
        Client.instance.myId=_myId;
        ClientSend.WelcomeReceived();

        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    // public static void UDPTest(Packet _packet){
    //     // ReadString()
    //     // ReadInt를 통해서 문자열의 길이를 읽고 그 후 읽은 길이만큼 문자열을 읽어서 리턴한다
    //     string _msg = _packet.ReadString();
    //     // ReceiveUDP-4 [] (문자열길이, 문자열 읽음)
    //     Debug.Log($"Received packet via UDP. Contains message: {_msg}");
    //     ClientSend.UDPTestReceived();
    // }

    public static void SpawnPlayer(Packet _packet){
        int _id = _packet.ReadInt();
        string _username = _packet.ReadString();
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();

        GameManager.instance.SpawnPlayer(_id, _username, _position, _rotation);
    }

    public static void PlayerPosition(Packet _packet){
        int _id = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        GameManager.players[_id].transform.position = _position;
    }

    public static void PlayerRotation(Packet _packet){
        int _id = _packet.ReadInt();
        Quaternion _rotation = _packet.ReadQuaternion();

        GameManager.players[_id].transform.rotation = _rotation;
    }
}
