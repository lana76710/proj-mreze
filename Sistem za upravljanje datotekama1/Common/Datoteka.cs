using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Datoteka
    {
        public string Naziv { get; set; }
        public string Autor { get; set; }
        public string PoslednjaIzmena { get; set; }
        public string Sadrzaj { get; set; }

        public Datoteka()
        {
        }

        public Datoteka(string naziv, string autor, string poslednjaIzmena, string sadrzaj)
        {
            Naziv = naziv;
            Autor = autor;
            PoslednjaIzmena = poslednjaIzmena;
            Sadrzaj = sadrzaj;
        }
    }
}
