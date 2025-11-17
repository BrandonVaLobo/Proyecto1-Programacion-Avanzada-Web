using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Models
{
    public class HistorialModel
    {
        public string Estudiante { get; set; }
        public string Identificacion { get; set; }
        public string Curso { get; set; }
        public string Cuatrimestre { get; set; }
        public decimal Nota { get; set; }
        public string Estado { get; set; }
        public DateTime FechaEvaluacion { get; set; }
    }
}