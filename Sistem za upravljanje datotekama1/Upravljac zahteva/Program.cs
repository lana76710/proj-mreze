using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Common;

namespace Upravlac_zahteva
{
    internal class Program
    {
        private const int RM_TCP_PORT = 7000;
        private const int SERVER_TCP_PORT = 6000;
        private const string SERVER_IP = "127.0.0.1";
        private const int BUFFER_SIZE = 8192;

        static void Main(string[] args)
        {
            Console.WriteLine("=== UPRAVLJAC ZAHTEVA (RM) ===");

            // listen za klijente
            Socket listenClients = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenClients.Bind(new IPEndPoint(IPAddress.Any, RM_TCP_PORT));
            listenClients.Listen(10);
            listenClients.Blocking = false;

            // konekcija ka serveru
            Socket rmToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            rmToServer.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_TCP_PORT));

            List<Socket> clients = new List<Socket>();
            Dictionary<Socket, string> korisnici = new Dictionary<Socket, string>();
            List<Zahtev> aktivniZahtevi = new List<Zahtev>();

            while (true)
            {
                List<Socket> readSockets = new List<Socket>();
                readSockets.Add(listenClients);
                readSockets.AddRange(clients);

                Socket.Select(readSockets, null, null, 1000000);

                foreach (Socket s in readSockets)
                {
                    // NOVI KLIJENT
                    if (s == listenClients)
                    {
                        Socket c = listenClients.Accept();
                        c.Blocking = false;
                        clients.Add(c);

                        byte[] ub = new byte[BUFFER_SIZE];
                        c.Blocking = true;
                        int ubc = c.Receive(ub);
                        c.Blocking = false;

                        string username = Encoding.UTF8.GetString(ub, 0, ubc);
                        korisnici[c] = username;

                        Console.WriteLine($"Klijent povezan: {username}");
                        continue;
                    }

                    // POSTOJEĆI KLIJENT
                    try
                    {
                        byte[] cmdBuf = new byte[BUFFER_SIZE];
                        int cmdBytes = s.Receive(cmdBuf);
                        if (cmdBytes == 0)
                        {
                            clients.Remove(s);
                            korisnici.Remove(s);
                            s.Close();
                            continue;
                        }

                        string cmd = Encoding.UTF8.GetString(cmdBuf, 0, cmdBytes);
                        string username = korisnici[s];

                        if (cmd == "DODAJ")
                        {
                            byte[] b = new byte[BUFFER_SIZE];
                            s.Blocking = true;
                            int n = s.Receive(b);
                            s.Blocking = false;

                            BinaryFormatter bf = new BinaryFormatter();
                            MemoryStream ms = new MemoryStream(b, 0, n);
                            Datoteka d = (Datoteka)bf.Deserialize(ms);
                            d.Autor = username;

                            rmToServer.Send(Encoding.UTF8.GetBytes("DODAJ"));
                            ms = new MemoryStream();
                            bf.Serialize(ms, d);
                            rmToServer.Send(ms.ToArray());

                            byte[] ok = new byte[BUFFER_SIZE];
                            int okb = rmToServer.Receive(ok);
                            s.Send(ok, okb, SocketFlags.None);
                        }
                        else if (cmd == "PROCITAJ")
                        {
                            byte[] nb = new byte[BUFFER_SIZE];
                            s.Blocking = true;
                            int n = s.Receive(nb);
                            s.Blocking = false;

                            string naziv = Encoding.UTF8.GetString(nb, 0, n);

                            rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                            rmToServer.Send(Encoding.UTF8.GetBytes(naziv));

                            byte[] status = new byte[BUFFER_SIZE];
                            int sb = rmToServer.Receive(status);
                            string st = Encoding.UTF8.GetString(status, 0, sb);

                            if (st == "ODBIJENO")
                            {
                                s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            }
                            else
                            {
                                byte[] obj = new byte[BUFFER_SIZE];
                                int ob = rmToServer.Receive(obj);
                                s.Send(obj, ob, SocketFlags.None);
                            }
                        }
                        else if (cmd == "IZMENI")
                        {
                            // primi Zahtev
                            byte[] b = new byte[BUFFER_SIZE];
                            s.Blocking = true;
                            int n = s.Receive(b);
                            s.Blocking = false;

                            BinaryFormatter bf = new BinaryFormatter();
                            MemoryStream ms = new MemoryStream(b, 0, n);
                            Zahtev z = (Zahtev)bf.Deserialize(ms);

                            // provera zauzetosti
                            bool zauzeto = false;
                            for (int i = 0; i < aktivniZahtevi.Count; i++)
                            {
                                if (aktivniZahtevi[i].NazivDatoteke == z.NazivDatoteke)
                                {
                                    zauzeto = true;
                                    break;
                                }
                            }

                            if (zauzeto)
                            {
                                s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                                continue;
                            }

                            aktivniZahtevi.Add(z);

                            // traži datoteku od servera
                            rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                            rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                            byte[] status = new byte[BUFFER_SIZE];
                            int sb = rmToServer.Receive(status);
                            string st = Encoding.UTF8.GetString(status, 0, sb);

                            if (st == "ODBIJENO")
                            {
                                s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                                aktivniZahtevi.Remove(z);
                                continue;
                            }

                            // primi Datoteku sa servera
                            byte[] obj = new byte[BUFFER_SIZE];
                            int ob = rmToServer.Receive(obj);

                            // prosledi klijentu
                            s.Send(obj, ob, SocketFlags.None);

                            // primi izmenjenu Datoteku od klijenta
                            byte[] b2 = new byte[BUFFER_SIZE];
                            s.Blocking = true;
                            int n2 = s.Receive(b2);
                            s.Blocking = false;

                            bf = new BinaryFormatter();
                            ms = new MemoryStream(b2, 0, n2);
                            Datoteka izmenjena = (Datoteka)bf.Deserialize(ms);

                            izmenjena.Autor = korisnici[s];

                            // prosledi serveru
                            rmToServer.Send(Encoding.UTF8.GetBytes("IZMENI"));
                            ms = new MemoryStream();
                            bf.Serialize(ms, izmenjena);
                            rmToServer.Send(ms.ToArray());

                            byte[] ok = new byte[BUFFER_SIZE];
                            int okb = rmToServer.Receive(ok);
                            s.Send(ok, okb, SocketFlags.None);

                            aktivniZahtevi.Remove(z);
                        }
                        else if (cmd == "UKLONI")
                        {
                            // primi Zahtev
                            byte[] b = new byte[BUFFER_SIZE];
                            s.Blocking = true;
                            int n = s.Receive(b);
                            s.Blocking = false;

                            BinaryFormatter bf = new BinaryFormatter();
                            MemoryStream ms = new MemoryStream(b, 0, n);
                            Zahtev z = (Zahtev)bf.Deserialize(ms);

                            // provera da li je zauzeto
                            bool zauzeto = false;
                            for (int i = 0; i < aktivniZahtevi.Count; i++)
                            {
                                if (aktivniZahtevi[i].NazivDatoteke == z.NazivDatoteke)
                                {
                                    zauzeto = true;
                                    break;
                                }
                            }

                            if (zauzeto)
                            {
                                s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                                continue;
                            }

                            aktivniZahtevi.Add(z);

                            // prosledi serveru
                            rmToServer.Send(Encoding.UTF8.GetBytes("UKLONI"));
                            rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                            byte[] ok = new byte[BUFFER_SIZE];
                            int okb = rmToServer.Receive(ok);

                            s.Send(ok, okb, SocketFlags.None);

                            aktivniZahtevi.Remove(z);
                        }
                    }

                    catch (SocketException)
                    {
                        clients.Remove(s);
                        korisnici.Remove(s);
                        s.Close();
                    }
                }
            }
        }
    }
}
