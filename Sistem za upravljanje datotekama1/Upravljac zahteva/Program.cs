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
        private const int RM_TCP_PORT = 7000;      // TCP za klijente
        private const int SERVER_TCP_PORT = 6000;  // TCP ka serveru
        private const string SERVER_IP = "127.0.0.1";
        private const int BUFFER_SIZE = 8192;

        static void Main(string[] args)
        {
            Console.WriteLine("=== UPRAVLJAC ZAHTEVA (RM) ===");

            // 1) TCP listen za klijente
            Socket listenClients = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenClients.Bind(new IPEndPoint(IPAddress.Any, RM_TCP_PORT));
            listenClients.Listen(10);
            Console.WriteLine($"TCP za klijente otvoren na portu {RM_TCP_PORT}");

            // 2) TCP connect ka serveru
            Socket rmToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            rmToServer.Connect(new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_TCP_PORT));
            Console.WriteLine($"Povezan sa serverom: {rmToServer.RemoteEndPoint}");

            // 3) Accept 1 klijent
            Console.WriteLine("Cekam klijenta...");
            Socket client = listenClients.Accept();
            Console.WriteLine("Klijent povezan.");

            // prvo korisnicko ime (string)
            byte[] userBuf = new byte[BUFFER_SIZE];
            int userBytes = client.Receive(userBuf);
            string username = Encoding.UTF8.GetString(userBuf, 0, userBytes);

            // lista aktivnih zahteva (IZMENA / BRISANJE)
            List<Zahtev> aktivniZahtevi = new List<Zahtev>();

            while (true)
            {
                // primi komandu
                byte[] cmdBuf = new byte[BUFFER_SIZE];
                int cmdBytes = client.Receive(cmdBuf);
                string cmd = Encoding.UTF8.GetString(cmdBuf, 0, cmdBytes);

                if (cmd == "DODAJ")
                {
                    // primi Datoteka
                    byte[] b = new byte[BUFFER_SIZE];
                    int n = client.Receive(b);

                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream(b, 0, n);
                    Datoteka d = (Datoteka)bf.Deserialize(ms);

                    d.Autor = username;

                    // prosledi serveru
                    rmToServer.Send(Encoding.UTF8.GetBytes("DODAJ"));

                    bf = new BinaryFormatter();
                    ms = new MemoryStream();
                    bf.Serialize(ms, d);
                    rmToServer.Send(ms.ToArray());

                    // potvrda servera -> klijentu
                    byte[] okBuf = new byte[BUFFER_SIZE];
                    int okBytes = rmToServer.Receive(okBuf);
                    client.Send(okBuf, okBytes, SocketFlags.None);
                }
                else if (cmd == "PROCITAJ")
                {
                    // primi naziv
                    byte[] nameBuf = new byte[BUFFER_SIZE];
                    int nb = client.Receive(nameBuf);
                    string naziv = Encoding.UTF8.GetString(nameBuf, 0, nb);

                    // server: PROCITAJ + naziv
                    rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                    rmToServer.Send(Encoding.UTF8.GetBytes(naziv));

                    // server prvo vraća "OK" ili "ODBIJENO"
                    byte[] statusBuf = new byte[BUFFER_SIZE];
                    int sb = rmToServer.Receive(statusBuf);
                    string status = Encoding.UTF8.GetString(statusBuf, 0, sb);

                    if (status == "ODBIJENO")
                    {
                        client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                    }
                    else
                    {
                        // primi objekat Datoteka i prosledi klijentu
                        byte[] objBuf = new byte[BUFFER_SIZE];
                        int ob = rmToServer.Receive(objBuf);

                        client.Send(objBuf, ob, SocketFlags.None);
                    }
                }
                else if (cmd == "IZMENI")
                {
                    // primi Zahtev
                    byte[] b = new byte[BUFFER_SIZE];
                    int n = client.Receive(b);

                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream(b, 0, n);
                    Zahtev z = (Zahtev)bf.Deserialize(ms);

                    // ===== ZADATAK 4: PROVERA "ZAUZETO" =====
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
                        client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                        continue;
                    }
                    // ========================================

                    // tek sad postaje aktivan zahtev
                    aktivniZahtevi.Add(z);

                    // traži datoteku od servera
                    rmToServer.Send(Encoding.UTF8.GetBytes("PROCITAJ"));
                    rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                    byte[] statusBuf = new byte[BUFFER_SIZE];
                    int sb = rmToServer.Receive(statusBuf);
                    string status = Encoding.UTF8.GetString(statusBuf, 0, sb);

                    if (status == "ODBIJENO")
                    {
                        client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                        aktivniZahtevi.Remove(z);
                    }
                    else
                    {
                        // primi Datoteka sa servera
                        byte[] objBuf = new byte[BUFFER_SIZE];
                        int ob = rmToServer.Receive(objBuf);

                        // prosledi klijentu (da je izmeni)
                        client.Send(objBuf, ob, SocketFlags.None);

                        // primi izmenjenu Datoteka od klijenta
                        byte[] b2 = new byte[BUFFER_SIZE];
                        int n2 = client.Receive(b2);

                        bf = new BinaryFormatter();
                        ms = new MemoryStream(b2, 0, n2);
                        Datoteka izmenjena = (Datoteka)bf.Deserialize(ms);

                        izmenjena.Autor = username;

                        // pošalji serveru IZMENI + objekat
                        rmToServer.Send(Encoding.UTF8.GetBytes("IZMENI"));

                        bf = new BinaryFormatter();
                        ms = new MemoryStream();
                        bf.Serialize(ms, izmenjena);
                        rmToServer.Send(ms.ToArray());

                        // odgovor servera -> klijentu
                        byte[] okBuf = new byte[BUFFER_SIZE];
                        int okBytes = rmToServer.Receive(okBuf);
                        client.Send(okBuf, okBytes, SocketFlags.None);

                        aktivniZahtevi.Remove(z);
                    }
                }
                else if (cmd == "UKLONI")
                {
                    // primi Zahtev
                    byte[] b = new byte[BUFFER_SIZE];
                    int n = client.Receive(b);

                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream(b, 0, n);
                    Zahtev z = (Zahtev)bf.Deserialize(ms);

                    // ===== ZADATAK 4: PROVERA "ZAUZETO" =====
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
                        client.Send(Encoding.UTF8.GetBytes("ODBIJENO"));
                        continue;
                    }
                    // ========================================

                    aktivniZahtevi.Add(z);

                    // server: UKLONI + naziv
                    rmToServer.Send(Encoding.UTF8.GetBytes("UKLONI"));
                    rmToServer.Send(Encoding.UTF8.GetBytes(z.NazivDatoteke));

                    // potvrda/ODBIJENO -> klijentu
                    byte[] okBuf = new byte[BUFFER_SIZE];
                    int okBytes = rmToServer.Receive(okBuf);
                    client.Send(okBuf, okBytes, SocketFlags.None);

                    aktivniZahtevi.Remove(z);
                }
            }
        }
    }
}
