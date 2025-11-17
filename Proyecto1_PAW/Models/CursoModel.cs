using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Models
{
    public class CursoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Creditos { get; set; }
        public int CuatrimestreId { get; set; }
        public string CuatrimestreNombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string CreadoPor { get; set; }
    }
}