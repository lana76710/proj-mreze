// ===================== Upravlac_zahteva/Program.cs =====================
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

            
            Socket listenClients = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenClients.Bind(new IPEndPoint(IPAddress.Any, RM_TCP_PORT));
            listenClients.Listen(10);

           
            Socket rmToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            rmToServer.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_TCP_PORT));

            List<Socket> clients = new List<Socket>();
            Dictionary<Socket, string> userByClient = new Dictionary<Socket, string>();
            Dictionary<Socket, Zahtev> pendingEdits = new Dictionary<Socket, Zahtev>();

            
            List<Zahtev> aktivniZahtevi = new List<Zahtev>();

            while (true)
            {
                List<Socket> readList = new List<Socket>();
                readList.Add(listenClients);
                for (int i = 0; i < clients.Count; i++) readList.Add(clients[i]);

                Socket.Select(readList, null, null, -1);

                for (int i = 0; i < readList.Count; i++)
                {
                    Socket s = readList[i];

                    if (s == listenClients)
                    {
                        Socket c = listenClients.Accept();
                        clients.Add(c);
                        Console.WriteLine("Klijent TCP povezan: " + c.RemoteEndPoint);


                       
                        byte[] userBuf = new byte[BUFFER_SIZE];
                        int userBytes = c.Receive(userBuf);
                        string username = Encoding.UTF8.GetString(userBuf, 0, userBytes);
                        userByClient[c] = username;

                        Console.WriteLine($"Korisnik '{username}' se prijavio.");
                        continue;
                    }

                   
                    byte[] cmdBuf = new byte[BUFFER_SIZE];
                    int cmdBytes = s.Receive(cmdBuf);
                    if (cmdBytes <= 0)
                    {
                        clients.Remove(s);
                        userByClient.Remove(s);
                        if (pendingEdits.TryGetValue(s, out Zahtev pending))
                        {
                            aktivniZahtevi.Remove(pending);
                            pendingEdits.Remove(s);
                        }
                        continue;
                    }

                    if (pendingEdits.TryGetValue(s, out Zahtev aktivnaIzmena))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(cmdBuf, 0, cmdBytes);
                        Datoteka izmenjena = (Datoteka)bf.Deserialize(ms);
                        izmenjena.Autor = userByClient[s];

                        rmToServer.Send(Encoding.UTF8.GetBytes("IZMENI"));
                        bf = new BinaryFormatter();
                        ms = new MemoryStream();
                        bf.Serialize(ms, izmenjena);
                        rmToServer.Send(ms.ToArray());

                        byte[] okBuf = new byte[BUFFER_SIZE];
                        int okBytes = rmToServer.Receive(okBuf);
                        s.Send(okBuf, okBytes, SocketFlags.None);

                        aktivniZahtevi.Remove(aktivnaIzmena);
                        pendingEdits.Remove(s);
                        continue;
                    }
                    string cmd = Encoding.UTF8.GetString(cmdBuf, 0, cmdBytes);
                    string usernameClient = userByClient[s];

                    if (cmd == "DODAJ")
                    {
                        byte[] b = new byte[BUFFER_SIZE];
                        int n = s.Receive(b);

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(b, 0, n);
                        Datoteka d = (Datoteka)bf.Deserialize(ms);

                        d.Autor = usernameClient;

                        rmToServer.Send(Encoding.UTF8.GetBytes("DODAJ"));
                        bf = new BinaryFormatter();
                        ms = new MemoryStream();
                        bf.Serialize(ms, d);
                        rmToServer.Send(ms.ToArray());

                        byte[] okBuf = new byte[BUFFER_SIZE];
                        int okBytes = rmToServer.Receive(okBuf);
                        s.Send(okBuf, okBytes, SocketFlags.None);
                    }
                    else if (cmd == "PROCITAJ")
                    {
                        byte[] nameBuf = new byte[BUFFER_SIZE];
                        int nb = s.Receive(nameBuf);
                        string naziv = Encoding.UTF8.GetString(nameBuf, 0, nb);

                        rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                        rmToServer.Send(Encoding.UTF8.GetBytes(naziv));

                        byte[] statusBuf = new byte[BUFFER_SIZE];
                        int sb = rmToServer.Receive(statusBuf);
                        string status = Encoding.UTF8.GetString(statusBuf, 0, sb);

                        if (status == "ODBIJENO")
                        {
                            s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                        }
                        else
                        {
                            byte[] objBuf = new byte[BUFFER_SIZE];
                            int ob = rmToServer.Receive(objBuf);
                            s.Send(objBuf, ob, SocketFlags.None);
                        }
                    }
                    else if (cmd == "IZMENI")
                    {
                        byte[] b = new byte[BUFFER_SIZE];
                        int n = s.Receive(b);

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(b, 0, n);
                        Zahtev z = (Zahtev)bf.Deserialize(ms);

                     
                        bool zauzeto = false;
                        for (int k = 0; k < aktivniZahtevi.Count; k++)
                            if (aktivniZahtevi[k].NazivDatoteke == z.NazivDatoteke) { zauzeto = true; break; }

                        if (zauzeto)
                        {
                            s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            continue;
                        }

                        aktivniZahtevi.Add(z);

                        rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                        rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                        byte[] statusBuf = new byte[BUFFER_SIZE];
                        int sb = rmToServer.Receive(statusBuf);
                        string status = Encoding.UTF8.GetString(statusBuf, 0, sb);

                        if (status == "ODBIJENO")
                        {
                            s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            aktivniZahtevi.Remove(z);
                        }
                        else
                        {
                            byte[] objBuf = new byte[BUFFER_SIZE];
                            int ob = rmToServer.Receive(objBuf);
                            s.Send(objBuf, ob, SocketFlags.None);
                            pendingEdits[s] = z;
                        }
                    }
                    else if (cmd == "UKLONI")
                    {
                        byte[] b = new byte[BUFFER_SIZE];
                        int n = s.Receive(b);

                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(b, 0, n);
                        Zahtev z = (Zahtev)bf.Deserialize(ms);

                        bool zauzeto = false;
                        for (int k = 0; k < aktivniZahtevi.Count; k++)
                            if (aktivniZahtevi[k].NazivDatoteke == z.NazivDatoteke) { zauzeto = true; break; }

                        if (zauzeto)
                        {
                            s.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                            continue;
                        }

                        aktivniZahtevi.Add(z);

                        rmToServer.Send(Encoding.UTF8.GetBytes("UKLONI"));
                        rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                        byte[] okBuf = new byte[BUFFER_SIZE];
                        int okBytes = rmToServer.Receive(okBuf);
                        s.Send(okBuf, okBytes, SocketFlags.None);

                        aktivniZahtevi.Remove(z);
                    }
                }
            }
        }
    }
}
