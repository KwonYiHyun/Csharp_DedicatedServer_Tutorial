﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using System.Runtime.Serialization;
using System.Xml.Linq;

public class ServerHandle : MonoBehaviour
{
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        int _clientIdCheck = _packet.ReadInt();
        // ReceiveTCP-4 [문자열길이 int 4바이트 / 문자열 바이트배열] (클라이언트id 읽음)

        string _username = _packet.ReadString();
        // ReadString()
        // ReadInt를 통해서 문자열의 길이를 읽고 그 후 읽은 길이만큼 문자열을 읽어서 리턴한다
        // ReceiveTCP-5 [] (문자열길이, 문자열읽음)

        Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}");
        if (_fromClient != _clientIdCheck)
        {
            Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
    }

    public static void UDPTestReceived(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();
        // ReceiveUDP-5 [] (문자열길이, 문자열읽음)

        Console.WriteLine($"Received packet via UDP. Contains message: {_msg}");
    }

    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        bool[] _inputs = new bool[_packet.ReadInt()];
        for (int i = 0; i < _inputs.Length; i++)
        {
            _inputs[i] = _packet.ReadBool();
        }
        Quaternion _rotation = _packet.ReadQuaternion();

        Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
    }

    public static void PlayerShoot(int _fromClient, Packet _packet){
        Vector3 _shootDirection = _packet.ReadVector3();

        Server.clients[_fromClient].player.Shoot(_shootDirection);
    }

    public static void unityChan(int _fromClient, Packet _packet){
        Debug.Log("Receive!");
        int _length = _packet.ReadInt();
        Debug.Log("length = " + _length);
        byte[] _model = _packet.ReadBytes(_length);

        unityC _unityc = new unityC();

        using(var memStream=new MemoryStream()){
            var binForm = new BinaryFormatter();
            // memStream.Write(_model, 0, _model.Length);
            // memStream.Seek(0, SeekOrigin.Begin);
            // _gameModel = binForm.Deserialize(memStream);
            // Instantiate((UnityEngine.Object)_gameModel, Vector3.zero, Quaternion.identity);

            memStream.Write(_model, 0, _model.Length);
            memStream.Seek(0, SeekOrigin.Begin);

            _unityc = (unityC)binForm.Deserialize(memStream);

            Instantiate(_unityc.model, Vector3.zero, Quaternion.identity);
        }
    }
}

[DataContract]
public class unityC {
    public GameObject model;
}