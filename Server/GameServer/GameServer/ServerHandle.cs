﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class ServerHandle
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
        }

        public static void UDPTestReceived(int _fromClient, Packet _packet)
        {
            string _msg = _packet.ReadString();
            // ReceiveUDP-5 [] (문자열길이, 문자열읽음)

            Console.WriteLine($"Received packet via UDP. Contains message: {_msg}");
        }
    }
}
