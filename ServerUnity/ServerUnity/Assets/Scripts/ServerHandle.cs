using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class ServerHandle
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
}
