using Proyecto1_PAW.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proyecto1_PAW.Controllers
{
    public class RendimientoController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["EstudianteActual"] == null)
                return RedirectToAction("Login", "Cuenta");

            return View();
        }

        [HttpGet]
        public JsonResult ObtenerRendimiento(string fechaInicio = null, string fechaFin = null, int? cuatrimestreId = null)
        {
            if (Session["EstudianteId"] == null)
                return Json(new { success = false, message = "No autenticado" }, JsonRequestBehavior.AllowGet);

            int estudianteId = (int)Session["EstudianteId"];
            var lista = new List<RendimientoModel>();

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"
                    SELECT c.Nombre AS Curso, cu.Nombre AS Cuatrimestre, e.Nota, e.FechaEvaluacion
                    FROM Evaluacion e
                    INNER JOIN Curso c ON e.CursoId = c.Id
                    INNER JOIN Cuatrimestre cu ON e.CuatrimestreId = cu.Id
                    WHERE e.EstudianteId = @estId";

                if (cuatrimestreId.HasValue)
                    sql += " AND e.CuatrimestreId = @cuatrimestreId";
                if (!string.IsNullOrEmpty(fechaInicio))
                    sql += " AND e.FechaEvaluacion >= @inicio";
                if (!string.IsNullOrEmpty(fechaFin))
                    sql += " AND e.FechaEvaluacion <= @fin";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@estId", estudianteId);
                    if (cuatrimestreId.HasValue)
                        cmd.Parameters.AddWithValue("@cuatrimestreId", cuatrimestreId.Value);
                    if (!string.IsNullOrEmpty(fechaInicio))
                        cmd.Parameters.AddWithValue("@inicio", DateTime.Parse(fechaInicio));
                    if (!string.IsNullOrEmpty(fechaFin))
                        cmd.Parameters.AddWithValue("@fin", DateTime.Parse(fechaFin));

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lista.Add(new RendimientoModel
                            {
                                Curso = rdr.GetString(0),
                                Cuatrimestre = rdr.GetString(1),
                                Nota = rdr.GetDecimal(2),
                                FechaEvaluacion = rdr.GetDateTime(3)
                            });
                        }
                    }
                }
            }

            return Json(lista, JsonRequestBehavior.AllowGet);
        }
    }
}