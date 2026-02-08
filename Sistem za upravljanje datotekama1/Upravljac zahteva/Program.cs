// ===================== Upravlac_zahteva/Program.cs =====================
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Common;

namespace Upravlac_zahteva
{
    internal class Program
    {
        private const int RM_TCP_PORT = 7000;
        private const int SERVER_TCP_PORT = 6000;
        private const string SERVER_IP = "127.0.0.1";
        private const int BUFFER_SIZE = 8192;

        private static List<Zahtev> aktivniZahtevi = new List<Zahtev>();
        private static object zahteviLock = new object();
        private static object serverLock = new object();

        private static Socket rmToServer;

        static void Main(string[] args)
        {
            Console.WriteLine("=== UPRAVLJAC ZAHTEVA (RM) ===");

            // TCP listen za klijente
            Socket listenClients = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenClients.Bind(new IPEndPoint(IPAddress.Any, RM_TCP_PORT));
            listenClients.Listen(10);
            Console.WriteLine($"TCP za klijente otvoren na portu {RM_TCP_PORT}");

            // TCP connect ka serveru
            rmToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            rmToServer.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_TCP_PORT));
            Console.WriteLine($"Povezan sa serverom: {rmToServer.RemoteEndPoint}");

            while (true)
            {
                Console.WriteLine("Cekam klijenta...");
                Socket client = listenClients.Accept();
                Console.WriteLine($"Klijent povezan: {client.RemoteEndPoint}");

                Thread t = new Thread(HandleClient);
                t.IsBackground = true;
                t.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            Socket client = (Socket)obj;

            try
            {
                // prvo korisnicko ime
                byte[] userBuf = new byte[BUFFER_SIZE];
                int userBytes = client.Receive(userBuf);
                string username = Encoding.UTF8.GetString(userBuf, 0, userBytes);

                while (true)
                {
                    byte[] cmdBuf = new byte[BUFFER_SIZE];
                    int cmdBytes = client.Receive(cmdBuf);
                    if (cmdBytes <= 0) break;

                    string cmd = Encoding.UTF8.GetString(cmdBuf, 0, cmdBytes);

                    if (cmd == "DODAJ")
                    {
                        byte[] b = new byte[BUFFER_SIZE];
                        int n = client.Receive(b);

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(b, 0, n);
                        Datoteka d = (Datoteka)bf.Deserialize(ms);

                        d.Autor = username;

                        lock (serverLock)
                        {
                            rmToServer.Send(Encoding.UTF8.GetBytes("DODAJ"));

                            bf = new BinaryFormatter();
                            ms = new MemoryStream();
                            bf.Serialize(ms, d);
                            rmToServer.Send(ms.ToArray());

                            byte[] okBuf = new byte[BUFFER_SIZE];
                            int okBytes = rmToServer.Receive(okBuf);
                            client.Send(okBuf, okBytes, SocketFlags.None);
                        }
                    }
                    else if (cmd == "PROCITAJ")
                    {
                        byte[] nameBuf = new byte[BUFFER_SIZE];
                        int nb = client.Receive(nameBuf);
                        string naziv = Encoding.UTF8.GetString(nameBuf, 0, nb);

                        lock (serverLock)
                        {
                            rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                            rmToServer.Send(Encoding.UTF8.GetBytes(naziv));

                            byte[] statusBuf = new byte[BUFFER_SIZE];
                            int sb = rmToServer.Receive(statusBuf);
                            string status = Encoding.UTF8.GetString(statusBuf, 0, sb);

                            if (status == "ODBIJENO")
                            {
                                client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            }
                            else
                            {
                                byte[] objBuf = new byte[BUFFER_SIZE];
                                int ob = rmToServer.Receive(objBuf);

                                client.Send(objBuf, ob, SocketFlags.None);
                            }
                        }
                    }
                    else if (cmd == "IZMENI")
                    {
                        byte[] b = new byte[BUFFER_SIZE];
                        int n = client.Receive(b);

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(b, 0, n);
                        Zahtev z = (Zahtev)bf.Deserialize(ms);

                        // ZADATAK 4 - provera zauzetosti
                        bool zauzeto = false;
                        lock (zahteviLock)
                        {
                            for (int i = 0; i < aktivniZahtevi.Count; i++)
                            {
                                if (aktivniZahtevi[i].NazivDatoteke == z.NazivDatoteke)
                                {
                                    zauzeto = true;
                                    break;
                                }
                            }

                            if (!zauzeto)
                                aktivniZahtevi.Add(z);
                        }

                        if (zauzeto)
                        {
                            Console.WriteLine($"ODBIJENO (zauzeto): {z.NazivDatoteke}");
                            client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            continue;
                        }

                        // uzmi datoteku od servera
                        byte[] objBufFromServer;

                        lock (serverLock)
                        {
                            rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                            rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                            byte[] statusBuf = new byte[BUFFER_SIZE];
                            int sb = rmToServer.Receive(statusBuf);
                            string status = Encoding.UTF8.GetString(statusBuf, 0, sb);

                            if (status == "ODBIJENO")
                            {
                                client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                                lock (zahteviLock) { aktivniZahtevi.Remove(z); }
                                continue;
                            }

                            objBufFromServer = new byte[BUFFER_SIZE];
                            int ob = rmToServer.Receive(objBufFromServer);

                            // prosledi klijentu
                            client.Send(objBufFromServer, ob, SocketFlags.None);
                        }

                        // primi izmenjenu datoteku od klijenta
                        byte[] b2 = new byte[BUFFER_SIZE];
                        int n2 = client.Receive(b2);

                        bf = new BinaryFormatter();
                        ms = new MemoryStream(b2, 0, n2);
                        Datoteka izmenjena = (Datoteka)bf.Deserialize(ms);
                        izmenjena.Autor = username;

                        lock (serverLock)
                        {
                            rmToServer.Send(Encoding.UTF8.GetBytes("IZMENI"));

                            bf = new BinaryFormatter();
                            ms = new MemoryStream();
                            bf.Serialize(ms, izmenjena);
                            rmToServer.Send(ms.ToArray());

                            byte[] okBuf = new byte[BUFFER_SIZE];
                            int okBytes = rmToServer.Receive(okBuf);
                            client.Send(okBuf, okBytes, SocketFlags.None);
                        }

                        lock (zahteviLock) { aktivniZahtevi.Remove(z); }
                    }
                    else if (cmd == "UKLONI")
                    {
                        byte[] b = new byte[BUFFER_SIZE];
                        int n = client.Receive(b);

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(b, 0, n);
                        Zahtev z = (Zahtev)bf.Deserialize(ms);

                        // ZADATAK 4 - provera zauzetosti
                        bool zauzeto = false;
                        lock (zahteviLock)
                        {
                            for (int i = 0; i < aktivniZahtevi.Count; i++)
                            {
                                if (aktivniZahtevi[i].NazivDatoteke == z.NazivDatoteke)
                                {
                                    zauzeto = true;
                                    break;
                                }
                            }

                            if (!zauzeto)
                                aktivniZahtevi.Add(z);
                        }

                        if (zauzeto)
                        {
                            Console.WriteLine($"ODBIJENO (zauzeto): {z.NazivDatoteke}");
                            client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            continue;
                        }

                        lock (serverLock)
                        {
                            rmToServer.Send(Encoding.UTF8.GetBytes("UKLONI"));
                            rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                            byte[] okBuf = new byte[BUFFER_SIZE];
                            int okBytes = rmToServer.Receive(okBuf);
                            client.Send(okBuf, okBytes, SocketFlags.None);
                        }

                        lock (zahteviLock) { aktivniZahtevi.Remove(z); }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RM exception: " + e.Message);
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }
    }
}
