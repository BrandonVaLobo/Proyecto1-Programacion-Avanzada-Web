using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Models
{
    public class AsignacionDocenteModel
    {
        public int Id { get; set; }
        public int DocenteId { get; set; }
        public int CursoId { get; set; }
        public string DocenteNombre { get; set; }
        public string CursoNombre { get; set; }
        public DateTime FechaAsignacion { get; set; }
    }
}