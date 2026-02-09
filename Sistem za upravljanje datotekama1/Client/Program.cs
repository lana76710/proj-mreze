// ===================== Client/Program.cs =====================
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Common;

namespace Client
{
    internal class Program
    {
        private const string SERVER_IP = "127.0.0.1";
       // private const int SERVER_UDP_PORT = 5000;
        private const int BUFFER_SIZE = 8192;

        static void Main(string[] args)
        {
            Console.Write("Unesite vase korisnicko ime: ");
            string username = Console.ReadLine();

            int serverPort=int.Parse(args[0]);

            // UDP socket
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(SERVER_IP), serverPort);

            // PRIJAVA
            string message = "PRIJAVA:" + username;
            byte[] msgBytes = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(msgBytes, serverEP);

            byte[] buffer = new byte[BUFFER_SIZE];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int bytes = udpSocket.ReceiveFrom(buffer, ref remoteEP);
            string response = Encoding.UTF8.GetString(buffer, 0, bytes);

            // "RM_TCP_PORT:7000"
            int rmPort = int.Parse(response.Split(':')[1]);

            // LISTA
            udpSocket.SendTo(Encoding.UTF8.GetBytes("LISTA"), serverEP);

     


            buffer = new byte[BUFFER_SIZE];
            bytes = udpSocket.ReceiveFrom(buffer, ref remoteEP);

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(buffer, 0, bytes);
            List<Datoteka> datoteke = (List<Datoteka>)bf.Deserialize(ms);

            for (int i = 0; i < datoteke.Count; i++)
                Console.WriteLine($"{datoteke[i].Naziv} ({datoteke[i].Autor}) poslednja promena: {datoteke[i].PoslednjaIzmena}");

            // TCP ka RM
            Socket rmSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            rmSocket.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), rmPort));

            // prvo username RM-u
            rmSocket.Send(Encoding.UTF8.GetBytes(username));

            while (true)
            {
                Console.WriteLine("\n1) PROCITAJ  2) DODAJ  3) IZMENI  4) UKLONI 5) STATISTIKA");
                Console.Write("Izbor: ");
                string izbor = Console.ReadLine();

                if (izbor == "1")
                {
                    rmSocket.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                    Console.Write("Naziv datoteke: ");
                    rmSocket.Send(Encoding.UTF8.GetBytes(Console.ReadLine()));

                    byte[] b = new byte[BUFFER_SIZE];
                    int n = rmSocket.Receive(b);
                    string maybe = Encoding.UTF8.GetString(b, 0, n);

                    if (maybe == "ODBIJENO")
                    {
                        Console.WriteLine("ODBIJENO");
                    }
                    else
                    {
                        // dobili smo objekat Datoteka
                        bf = new BinaryFormatter();
                        ms = new MemoryStream(b, 0, n);
                        Datoteka d = (Datoteka)bf.Deserialize(ms);

                        Console.WriteLine(d.Sadrzaj);
                    }
                }
                else if (izbor == "2")
                {
                    rmSocket.Send(Encoding.UTF8.GetBytes("DODAJ"));

                    Datoteka d = new Datoteka();
                    Console.Write("Naziv: ");
                    d.Naziv = Console.ReadLine();
                    Console.Write("Sadrzaj: ");
                    d.Sadrzaj = Console.ReadLine();
                    d.Autor = "";            // RM dopunjava
                    d.PoslednjaIzmena = "";   // server dopunjava

                    bf = new BinaryFormatter();
                    ms = new MemoryStream();
                    bf.Serialize(ms, d);
                    rmSocket.Send(ms.ToArray());

                    byte[] ok = new byte[BUFFER_SIZE];
                    int okBytes = rmSocket.Receive(ok);
                    Console.WriteLine(Encoding.UTF8.GetString(ok, 0, okBytes));
                }
                else if (izbor == "3")
                {
                    rmSocket.Send(Encoding.UTF8.GetBytes("IZMENI"));

                    Zahtev z = new Zahtev();
                    Console.Write("Naziv datoteke: ");
                    z.NazivDatoteke = Console.ReadLine();
                    z.Operacija = Operacije.Izmena;

                    bf = new BinaryFormatter();
                    ms = new MemoryStream();
                    bf.Serialize(ms, z);
                    rmSocket.Send(ms.ToArray());

                    byte[] b = new byte[BUFFER_SIZE];
                    int n = rmSocket.Receive(b);
                    string maybe = Encoding.UTF8.GetString(b, 0, n);

                    if (maybe == "ODBIJENO")
                    {
                        Console.WriteLine("ODBIJENO");
                    }
                    else
                    {
                        bf = new BinaryFormatter();
                        ms = new MemoryStream(b, 0, n);
                        Datoteka d = (Datoteka)bf.Deserialize(ms);
                        //zadatak 6
                        Console.WriteLine("Trenutni sadrzaj: " + d.Sadrzaj);
                        Console.WriteLine("1) Zameni ceo sadrzaj");
                        Console.WriteLine("2) Dodaj na postojeci sadrzaj");
                        Console.Write("Izbor: ");
                        string izborIzmene = Console.ReadLine();

                        if (izborIzmene == "1")
                        {
                            Console.Write("Novi sadrzaj: ");
                            d.Sadrzaj = Console.ReadLine();
                        }
                        else if (izborIzmene == "2")
                        {
                            Console.Write("Tekst za dodavanje: ");
                            string dodatak = Console.ReadLine();
                            d.Sadrzaj += dodatak;
                        }

                        bf = new BinaryFormatter();
                        ms = new MemoryStream();
                        bf.Serialize(ms, d);
                        rmSocket.Send(ms.ToArray());
                        byte[] ok = new byte[BUFFER_SIZE];
                        int okBytes = rmSocket.Receive(ok);
                        Console.WriteLine(Encoding.UTF8.GetString(ok, 0, okBytes));
                    }
                }
                else if (izbor == "4")
                {
                    rmSocket.Send(Encoding.UTF8.GetBytes("UKLONI"));

                    Zahtev z = new Zahtev();
                    Console.Write("Naziv datoteke: ");
                    z.NazivDatoteke = Console.ReadLine();
                    z.Operacija = Operacije.Brisanje;

                    bf = new BinaryFormatter();
                    ms = new MemoryStream();
                    bf.Serialize(ms, z);
                    rmSocket.Send(ms.ToArray());

                    byte[] ok = new byte[BUFFER_SIZE];
                    int okBytes = rmSocket.Receive(ok);
                    Console.WriteLine(Encoding.UTF8.GetString(ok, 0, okBytes));
                }
                else if (izbor == "5")
                {
                    udpSocket.SendTo(Encoding.UTF8.GetBytes("STATISTIKA"), serverEP);

                    byte[] buffer2 = new byte[BUFFER_SIZE];
                    EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    int bytes2 = udpSocket.ReceiveFrom(buffer2, ref ep);

                     bf = new BinaryFormatter();
                     ms = new MemoryStream(buffer2, 0, bytes2);
                    Statistika s = (Statistika)bf.Deserialize(ms);

                    Console.WriteLine("\n--- STATISTIKA ---");
                    foreach (var par in s.BrojDatotekaPoAutoru)
                    {
                        Console.WriteLine($"{par.Key}: {par.Value}");
                    }

                    Console.WriteLine($"Ukupna velicina: {s.UkupnaVelicina} bajtova");
                }
            }
        }
    }
}
