using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        private const int UDP_PORT = 5000;   // UDP prijava klijenata
        private const int TCP_PORT = 6000;   // TCP veza sa RM
        private const int RM_TCP_PORT = 7000; // RM port za klijente (server šalje klijentu)

        private const int BUFFER_SIZE = 1024;

        static void Main(string[] args)
        {
            Console.WriteLine("=== SERVER (Repozitorijum) ===");

           
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));
            Console.WriteLine($"UDP otvoren na portu {UDP_PORT}");

            
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, TCP_PORT));
            listenSocket.Listen(10);
            Console.WriteLine($"TCP otvoren na portu {TCP_PORT} (cekam RM...)"); 

         
            Socket rmSocket = listenSocket.Accept(); 
            Console.WriteLine($"RM povezan: {rmSocket.RemoteEndPoint}");

         
            byte[] buffer = new byte[BUFFER_SIZE];

            while (true)
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int bytes = udpSocket.ReceiveFrom(buffer, ref remoteEP); 
                string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

                Console.WriteLine($"UDP primljeno od {remoteEP}: {msg}");

                if (msg.StartsWith("PRIJAVA"))
                {
                    
                    string response = $"RM_TCP_PORT:{RM_TCP_PORT}";
                    byte[] respBytes = Encoding.UTF8.GetBytes(response);

                    udpSocket.SendTo(respBytes, remoteEP); 
                    Console.WriteLine($"UDP odgovor poslat: {response}");
                }
            }
        }
    }
}
