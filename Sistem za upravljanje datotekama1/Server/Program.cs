using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Common;

namespace Server
{
    internal class Program
    {
        private const int UDP_PORT = 5000; // prijava klijenata
        private const int TCP_PORT = 6000; // komunikacija sa upravljačem zahteva

        static List<Datoteka> datoteke = new List<Datoteka>();

        static void Main(string[] args)
        {
            Console.WriteLine("=== SERVER (Repozitorijum) ===");

            // UDP utičnica (prijava klijenata)
            UdpClient udp = new UdpClient(UDP_PORT);
            Console.WriteLine($"UDP otvoren na portu {UDP_PORT}");

            // Primanje PRIJAVA poruka (jednostavno, minimalno)
            Console.WriteLine("Cekam PRIJAVA klijenata...");

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);

            // primamo samo jednu poruku za test
            byte[] receivedBytes = udp.Receive(ref remoteEP);
            string message = System.Text.Encoding.UTF8.GetString(receivedBytes);

            Console.WriteLine($"Primljena poruka od {remoteEP}: {message}");

            // odgovor klijentu sa TCP portom za RM (minimalno)
            string response = "TCP_PORT:" + TCP_PORT;
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
            udp.Send(responseBytes, responseBytes.Length, remoteEP);

            Console.WriteLine("Odgovor poslat klijentu. Test PRIJAVA gotov!");


            // TCP utičnica (RM konekcija)
            TcpListener tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
            tcpListener.Start();
            Console.WriteLine($"TCP otvoren na portu {TCP_PORT} (ceka RM...)");

            // Prihvati RM vezu (za konfiguraciju je dovoljno da se poveze)
            TcpClient rm = tcpListener.AcceptTcpClient();
            Console.WriteLine("RM se povezao na server!");

            Console.WriteLine("Konfiguracija gotova. (Za sada ne obradjujem poruke u zadatku 2)");
            Console.ReadLine();
        }
    }
}
