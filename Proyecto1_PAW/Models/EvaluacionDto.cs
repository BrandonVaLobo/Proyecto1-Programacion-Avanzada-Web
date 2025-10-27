using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Models
{
    public class EvaluacionDto
    {
        public int EstudianteId { get; set; }
        public int CursoId { get; set; }
        public int CuatrimestreId { get; set; }
        public decimal Nota { get; set; }
        public string Observaciones { get; set; }
        public string Participacion { get; set; }
        public string Estado { get; set; }
    }
}