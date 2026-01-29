using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        private const int SERVER_UDP_PORT = 5000;
        private const string SERVER_IP = "127.0.0.1";
        static void Main(string[] args)
        {
            Console.Write("Unesite vase korisnicko ime: ");
            string username = Console.ReadLine();

            using (UdpClient udp = new UdpClient())
            {
                udp.Connect(SERVER_IP, SERVER_UDP_PORT);

                // posalji PRIJAVA poruku
                string message = "PRIJAVA:" + username;
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                udp.Send(bytes, bytes.Length);

                // primi odgovor
                var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udp.Receive(ref remoteEP);
                string response = Encoding.UTF8.GetString(receivedBytes);

                Console.WriteLine($"Od servera primljen odgovor: {response}");
            }

            Console.WriteLine("Test PRIJAVA zavrsen. Enter za izlaz.");
            Console.ReadLine();
        }
    }
}

