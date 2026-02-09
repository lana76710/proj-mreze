// ===================== Server/Program.cs =====================
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Common;

namespace Server
{
    internal class Program
    {
        private const int UDP_PORT = 5000;
        private const int TCP_PORT = 6000;
        private const int RM_TCP_PORT = 7000;
        private const int BUFFER_SIZE = 8192;

        static void Main(string[] args)
        {
            Console.WriteLine("=== SERVER (Repozitorijum) ===");

            List<Datoteka> datoteke = new List<Datoteka>();
            datoteke.Add(new Datoteka("test.txt", "server", DateTime.Now.ToString(), "primer"));

            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));
            Console.WriteLine($"UDP otvoren na portu {UDP_PORT}");

            System.Threading.Thread udpThread = new System.Threading.Thread(() =>
            {
                byte[] buffer = new byte[BUFFER_SIZE];

                while (true)
                {
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int bytes = udpSocket.ReceiveFrom(buffer, ref remoteEP);
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

                    if (msg.StartsWith("PRIJAVA"))
                    {
                        string response = $"RM_TCP_PORT:{RM_TCP_PORT}";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(response), remoteEP);
                    }
                    else if (msg == "LISTA")
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream();
                        bf.Serialize(ms, datoteke);
                        udpSocket.SendTo(ms.ToArray(), remoteEP);
                    }
                    else if (msg == "STATISTIKA")
                    {
                        Statistika s = new Statistika();
                        s.BrojDatotekaPoAutoru = new Dictionary<string, int>();
                        s.UkupnaVelicina = 0;

                        for (int i = 0; i < datoteke.Count; i++)
                        {
                            Datoteka d = datoteke[i];

                            if (!s.BrojDatotekaPoAutoru.ContainsKey(d.Autor))
                                s.BrojDatotekaPoAutoru[d.Autor] = 0;
                            s.BrojDatotekaPoAutoru[d.Autor]++;

                            if (d.Sadrzaj != null)
                                s.UkupnaVelicina += d.Sadrzaj.Length;
                        }

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream();
                        bf.Serialize(ms, s);
                        udpSocket.SendTo(ms.ToArray(), remoteEP);
                    }
                }
            });
            udpThread.IsBackground = true;
            udpThread.Start();

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, TCP_PORT));
            listenSocket.Listen(10);
            Console.WriteLine($"TCP otvoren na portu {TCP_PORT} (cekam RM...)");

            Socket rmSocket = listenSocket.Accept();
            Console.WriteLine($"RM povezan: {rmSocket.RemoteEndPoint}");

            while (true)
            {
                byte[] cmdBuf = new byte[BUFFER_SIZE];
                int cmdBytes = rmSocket.Receive(cmdBuf);
                string cmd = Encoding.UTF8.GetString(cmdBuf, 0, cmdBytes);

                if (cmd == "DODAJ")
                {
                    byte[] b = new byte[BUFFER_SIZE];
                    int n = rmSocket.Receive(b);

                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream(b, 0, n);
                    Datoteka d = (Datoteka)bf.Deserialize(ms);

                    d.PoslednjaIzmena = DateTime.Now.ToString();
                    datoteke.Add(d);

                    rmSocket.Send(Encoding.UTF8.GetBytes("OK"));
                }
                else if (cmd == "PROCITAJ")
                {
                    byte[] nameBuf = new byte[BUFFER_SIZE];
                    int nb = rmSocket.Receive(nameBuf);
                    string naziv = Encoding.UTF8.GetString(nameBuf, 0, nb);

                    Datoteka found = null;
                    for (int i = 0; i < datoteke.Count; i++)
                        if (datoteke[i].Naziv == naziv) { found = datoteke[i]; break; }

                    if (found == null)
                    {
                        rmSocket.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                    }
                    else
                    {
                        rmSocket.Send(Encoding.UTF8.GetBytes("OK"));

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream();
                        bf.Serialize(ms, found);
                        rmSocket.Send(ms.ToArray());
                    }
                }
                else if (cmd == "IZMENI")
                {
                    byte[] b = new byte[BUFFER_SIZE];
                    int n = rmSocket.Receive(b);

                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream(b, 0, n);
                    Datoteka nova = (Datoteka)bf.Deserialize(ms);

                    Datoteka stara = null;
                    for (int i = 0; i < datoteke.Count; i++)
                        if (datoteke[i].Naziv == nova.Naziv) { stara = datoteke[i]; break; }

                    if (stara == null)
                    {
                        rmSocket.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                    }
                    else
                    {
                        stara.Sadrzaj = nova.Sadrzaj;
                        stara.Autor = nova.Autor;
                        stara.PoslednjaIzmena = DateTime.Now.ToString();
                        rmSocket.Send(Encoding.UTF8.GetBytes("OK"));
                    }
                }
                else if (cmd == "UKLONI")
                {
                    byte[] nameBuf = new byte[BUFFER_SIZE];
                    int nb = rmSocket.Receive(nameBuf);
                    string naziv = Encoding.UTF8.GetString(nameBuf, 0, nb);

                    Datoteka found = null;
                    for (int i = 0; i < datoteke.Count; i++)
                        if (datoteke[i].Naziv == naziv) { found = datoteke[i]; break; }

                    if (found == null)
                    {
                        rmSocket.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                    }
                    else
                    {
                        datoteke.Remove(found);
                        rmSocket.Send(Encoding.UTF8.GetBytes("OK"));
                    }
                }
            }
        }
    }
}
