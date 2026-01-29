using System;

namespace Common
{
    public enum Operacija
    {
        Dodavanje,
        Izmena,
        Uklanjanje
    }

    [Serializable]
    public class Zahtev
    {
        public string PutanjaDoDatoteke { get; set; }
        public Operacija Operacija { get; set; }
    }
}
