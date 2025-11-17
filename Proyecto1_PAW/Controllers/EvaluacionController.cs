using Proyecto1_PAW.Models;
using Proyecto1_PAW.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proyecto1_PAW.Controllers
{
    public class EvaluacionController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["DocenteActual"] == null)
                return RedirectToAction("Login", "Cuenta");
            return View();
        }

        [HttpGet]
        public JsonResult Buscar(string filtro)
        {
            var lista = new List<object>();
            if (string.IsNullOrWhiteSpace(filtro))
                return Json(lista, JsonRequestBehavior.AllowGet);

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT TOP 10 e.Id, e.Nombre, e.Apellidos, e.Identificacion,
                            c.Id AS CuatrimestreId, c.Nombre AS CuatrimestreNombre
                            FROM Estudiante e
                            JOIN Cuatrimestre c ON e.CuatrimestreId = c.Id
                            WHERE e.Nombre LIKE @f OR e.Apellidos LIKE @f OR e.Identificacion LIKE @f
                            ORDER BY e.Nombre";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@f", "%" + filtro + "%");
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lista.Add(new
                            {
                                id = rdr.GetInt32(0),
                                nombre = rdr.GetString(1),
                                apellidos = rdr.GetString(2),
                                identificacion = rdr.GetString(3),
                                cuatrimestreId = rdr.GetInt32(4),
                                cuatrimestre = rdr.GetString(5)
                            });
                        }
                    }
                }
            }
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarEvaluacion(EvaluacionDto dto)
        {
            if (dto == null || dto.Nota < 0 || dto.Nota > 100 || string.IsNullOrWhiteSpace(dto.Participacion) || string.IsNullOrWhiteSpace(dto.Estado))
            {
                return Json(new { success = false, message = "Datos inválidos o incompletos." });
            }

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var check = new SqlCommand("SELECT COUNT(*) FROM Evaluacion WHERE EstudianteId=@E AND CursoId=@C AND CuatrimestreId=@Q", conn, tran))
                        {
                            check.Parameters.AddWithValue("@E", dto.EstudianteId);
                            check.Parameters.AddWithValue("@C", dto.CursoId);
                            check.Parameters.AddWithValue("@Q", dto.CuatrimestreId);
                            int exists = (int)check.ExecuteScalar();
                            if (exists > 0)
                            {
                                tran.Rollback();
                                return Json(new { success = false, message = "Ya existe una evaluación registrada para este curso y cuatrimestre." });
                            }
                        }

                        string sql = @"INSERT INTO Evaluacion (EstudianteId, CursoId, CuatrimestreId, Nota, Observaciones, Participacion, Estado)
                                       VALUES (@E,@C,@Q,@Nota,@Obs,@Part,@Est)";
                        using (var cmd = new SqlCommand(sql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@E", dto.EstudianteId);
                            cmd.Parameters.AddWithValue("@C", dto.CursoId);
                            cmd.Parameters.AddWithValue("@Q", dto.CuatrimestreId);
                            cmd.Parameters.AddWithValue("@Nota", dto.Nota);
                            cmd.Parameters.AddWithValue("@Obs", (object)dto.Observaciones ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Part", dto.Participacion);
                            cmd.Parameters.AddWithValue("@Est", dto.Estado);
                            cmd.ExecuteNonQuery();
                        }

                        BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Asigno estudiante Id " + dto.EstudianteId + " al curso Id " + dto.CursoId, "Curso");

                        tran.Commit();
                        return Json(new { success = true, message = "Evaluación registrada correctamente." });
                    }
                    catch
                    {
                        BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Asignar estudiante Id " + dto.EstudianteId + " al curso Id " + dto.CursoId, "Curso");
                        tran.Rollback();
                        return Json(new { success = false, message = "Error al guardar la evaluación." });
                    }
                }
            }
        }
    }
}