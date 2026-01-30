using System;
using System.Net;
using System.Net.Sockets;

namespace Upravlac_zahteva
{
    internal class Program
    {
        private const int RM_TCP_PORT = 7000;     // TCP za klijente
        private const int SERVER_TCP_PORT = 6000; // TCP ka serveru
        private const string SERVER_IP = "127.0.0.1";

        static void Main(string[] args)
        {
            Console.WriteLine("=== UPRAVLJAC ZAHTEVA ===");

            // 1) TCP listen socket za klijente: Bind + Listen
            Socket listenClients = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenClients.Bind(new IPEndPoint(IPAddress.Any, RM_TCP_PORT));
            listenClients.Listen(10);
            Console.WriteLine($"TCP za klijente otvoren na portu {RM_TCP_PORT}");

            // 2) TCP client socket ka serveru: Connect
            Socket rmToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            rmToServer.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_TCP_PORT)); // Connect :contentReference[oaicite:6]{index=6}
            Console.WriteLine($"Povezan sa serverom {rmToServer.RemoteEndPoint}");

            Console.WriteLine("Konfiguracija za zadatak 2 gotova.");
            Console.ReadLine();
        }
    }
}
