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
    public class HistorialController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Rol"] == null ||
               (!Session["Rol"].ToString().Contains("Docente") && !Session["Rol"].ToString().Contains("Coordinador")))
                return RedirectToAction("AccesoDenegado", "Cuenta");

            return View();
        }

        [HttpGet]
        public JsonResult BuscarEstudiante(string filtro)
        {
            var lista = new List<dynamic>();

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT TOP 10 Id, Nombre, Apellidos, Identificacion
                               FROM Estudiante
                               WHERE Nombre LIKE @f OR Apellidos LIKE @f OR Identificacion LIKE @f";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@f", "%" + filtro + "%");
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lista.Add(new
                            {
                                Id = rdr.GetInt32(0),
                                Nombre = rdr.GetString(1) + " " + rdr.GetString(2),
                                Identificacion = rdr.GetString(3)
                            });
                        }
                    }
                }
            }
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerHistorial(int estudianteId)
        {
            var lista = new List<HistorialModel>();

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        e.Nombre + ' ' + e.Apellidos AS Estudiante,
                        e.Identificacion,
                        c.Nombre AS Curso,
                        cu.Nombre AS Cuatrimestre,
                        ev.Nota,
                        ev.Estado,
                        ev.FechaEvaluacion
                    FROM Evaluacion ev
                    INNER JOIN Estudiante e ON ev.EstudianteId = e.Id
                    INNER JOIN Curso c ON ev.CursoId = c.Id
                    INNER JOIN Cuatrimestre cu ON ev.CuatrimestreId = cu.Id
                    WHERE e.Id = @id
                    ORDER BY cu.Id, ev.FechaEvaluacion DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", estudianteId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lista.Add(new HistorialModel
                            {
                                Estudiante = rdr.GetString(0),
                                Identificacion = rdr.GetString(1),
                                Curso = rdr.GetString(2),
                                Cuatrimestre = rdr.GetString(3),
                                Nota = rdr.GetDecimal(4),
                                Estado = rdr.GetString(5),
                                FechaEvaluacion = rdr.GetDateTime(6)
                            });
                        }
                    }
                }
            }

            return Json(lista, JsonRequestBehavior.AllowGet);
        }
    }
}