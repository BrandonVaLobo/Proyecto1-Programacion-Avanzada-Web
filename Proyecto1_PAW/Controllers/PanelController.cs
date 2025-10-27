using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proyecto1_PAW.Controllers
{
    public class PanelController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["DocenteActual"] == null)
                return RedirectToAction("Login", "Cuenta");

            ViewBag.Usuario = Session["DocenteActual"].ToString();
            return View();
        }

        public ActionResult Seguimiento()
        {
            if (Session["DocenteActual"] == null)
                return RedirectToAction("Login", "Cuenta");

            ViewBag.Cuatrimestres = ObtenerLista("SELECT Id, Nombre FROM Cuatrimestre");
            ViewBag.Cursos = ObtenerLista("SELECT Id, Nombre FROM Curso");
            return View();
        }

        [HttpGet]
        public JsonResult ObtenerEstadisticas(int cuatrimestreId, int cursoId)
        {
            if (cuatrimestreId <= 0 || cursoId <= 0)
                return Json(new { success = false, message = "Debe seleccionar cuatrimestre y curso válidos." }, JsonRequestBehavior.AllowGet);

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT 
                                COUNT(eva.Id) AS TotalEvaluaciones,
                                SUM(CASE WHEN eva.Participacion = 'Alta' THEN 1 ELSE 0 END) * 100.0 / COUNT(eva.Id) AS PorcentajeParticipacionAlta,
                                SUM(CASE WHEN eva.Estado = 'Aprobado' THEN 1 ELSE 0 END) * 100.0 / COUNT(eva.Id) AS PorcentajeAprobados,
                                SUM(CASE WHEN eva.Estado = 'Reprobado' THEN 1 ELSE 0 END) * 100.0 / COUNT(eva.Id) AS PorcentajeReprobados
                            FROM Evaluacion eva
                            WHERE eva.CuatrimestreId = @Q AND eva.CursoId = @C";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Q", cuatrimestreId);
                    cmd.Parameters.AddWithValue("@C", cursoId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read() && rdr["TotalEvaluaciones"] != DBNull.Value)
                        {
                            return Json(new
                            {
                                success = true,
                                total = rdr.GetInt32(0),
                                participacion = rdr.IsDBNull(1) ? 0 : Convert.ToDouble(rdr[1]),
                                aprobados = rdr.IsDBNull(2) ? 0 : Convert.ToDouble(rdr[2]),
                                reprobados = rdr.IsDBNull(3) ? 0 : Convert.ToDouble(rdr[3])
                            }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
            return Json(new { success = false, message = "No hay evaluaciones registradas para esa combinación." }, JsonRequestBehavior.AllowGet);
        }

        private List<KeyValuePair<int, string>> ObtenerLista(string sql)
        {
            var lista = new List<KeyValuePair<int, string>>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        lista.Add(new KeyValuePair<int, string>(rdr.GetInt32(0), rdr.GetString(1)));
                }
            }
            return lista;
        }
    }
}
