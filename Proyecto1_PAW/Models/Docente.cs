using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Models
{
    public class Docente
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; } 
        public bool Activo { get; set; } = true;
    }
}