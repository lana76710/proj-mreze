using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_UDP_PORT = 5000;
        private const int BUFFER_SIZE = 1024;

        static void Main(string[] args)
        {
            Console.Write("Unesite vase korisnicko ime: ");
            string username = Console.ReadLine();

            // UDP socket
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_UDP_PORT);

            // SendTo PRIJAVA
            string message = "PRIJAVA:" + username;
            byte[] msgBytes = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(msgBytes, serverEP); // SendTo :contentReference[oaicite:7]{index=7}
            Console.WriteLine("Poslata PRIJAVA (UDP).");

            // ReceiveFrom odgovor
            byte[] buffer = new byte[BUFFER_SIZE];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int bytes = udpSocket.ReceiveFrom(buffer, ref remoteEP); // ReceiveFrom :contentReference[oaicite:8]{index=8}
            string response = Encoding.UTF8.GetString(buffer, 0, bytes);

            Console.WriteLine($"Od servera: {response}");

            udpSocket.Close();
            Console.WriteLine("Enter za izlaz...");
            Console.ReadLine();
        }
    }
}
