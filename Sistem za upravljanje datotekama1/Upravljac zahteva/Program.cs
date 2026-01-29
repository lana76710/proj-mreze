using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Common;

namespace RequestManager
{
    class UpravljačZahtevima
    {
        static List<Zahtev> aktivniZahtevi = new List<Zahtev>();

        static void Main()
        {
            // TCP za klijente
            TcpListener tcpKlijenti = new TcpListener(IPAddress.Any, 7000);
            tcpKlijenti.Start();
            Console.WriteLine("Upravljač zahteva: TCP za klijente otvoren na portu 7000");

            // TCP konekcija ka repozitorijumu
            TcpClient repoClient = new TcpClient();
            repoClient.Connect("127.0.0.1", 6000);
            Console.WriteLine("Upravljač zahteva: povezan sa Repozitorijumom");

            Console.ReadLine();
        }
    }
}
