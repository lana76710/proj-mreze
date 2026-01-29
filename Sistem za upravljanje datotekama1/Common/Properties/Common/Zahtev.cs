using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Zahtev
    {
            public string NazivDatoteke { get; set; }
            public Operacija Operacija { get; set; }

            public Zahtev()
            {
            }

            public Zahtev(string nazivDatoteke, Operacija operacija)
            {
                NazivDatoteke = nazivDatoteke;
                Operacija = operacija;
            }
        }
}
