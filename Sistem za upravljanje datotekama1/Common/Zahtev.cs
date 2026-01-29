using System;

namespace Common
{
    [Serializable]
    public class Zahtev
    {
        public string PutanjaDoDatoteke { get; set; } = "";
        public Operacija Operacija { get; set; }
    }
}
