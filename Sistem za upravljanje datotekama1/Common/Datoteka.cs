using System;

namespace Common
{
    [Serializable]
    public class Datoteka
    {
        public string NazivDatoteke { get; set; } = "";
        public string Autor { get; set; } = "";
        public string VremePoslednjePromene { get; set; } = "";
        public string Sadrzaj { get; set; } = "";
    }
}
