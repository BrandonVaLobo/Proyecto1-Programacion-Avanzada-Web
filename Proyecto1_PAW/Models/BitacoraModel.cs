using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Models
{
    public class BitacoraModel
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Accion { get; set; }
        public string Modulo { get; set; }
        public string Fecha { get; set; }
        public string IP { get; set; }
    }
}