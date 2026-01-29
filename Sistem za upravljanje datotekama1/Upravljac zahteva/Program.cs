using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;


namespace Upravljac_zahteva
{
    internal class Program
    {
        private const int RM_TCP_PORT = 7000;  // RM sluša klijente
        private const int SERVER_TCP_PORT = 6000; // RM se kači na Server
        private const string SERVER_IP = "127.0.0.1";

        //static List<Zahtev> aktivniZahtevi = new List<Zahtev>();

        static void Main(string[] args)
        {
            Console.WriteLine("=== UPRAVLJAC ZAHTEVA ===");

            // TCP utičnica za klijente
            TcpListener clientListener = new TcpListener(IPAddress.Any, RM_TCP_PORT);
            clientListener.Start();
            Console.WriteLine($"TCP za klijente otvoren na portu {RM_TCP_PORT}");

            // TCP veza ka repozitorijumu
            TcpClient serverConn = new TcpClient();
            serverConn.Connect(SERVER_IP, SERVER_TCP_PORT);
            Console.WriteLine("Povezan sa Repozitorijumom (Serverom)");

            Console.WriteLine("Konfiguracija gotova. (Za sada ne obradjujem poruke u zadatku 2)");
            Console.ReadLine();
        }
    }
}
