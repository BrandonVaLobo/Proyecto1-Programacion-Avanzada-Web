using Proyecto1_PAW.Models;
using Proyecto1_PAW.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Proyecto1_PAW.Controllers
{
    public class DocenteCursoController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            var lista = new List<AsignacionDocenteModel>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT dc.Id, d.Usuario AS DocenteNombre, c.Nombre AS CursoNombre, dc.FechaAsignacion
                               FROM DocenteCurso dc
                               INNER JOIN Docentes d ON dc.DocenteId = d.Id
                               INNER JOIN Curso c ON dc.CursoId = c.Id
                               ORDER BY dc.FechaAsignacion DESC";

                using (var cmd = new SqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        lista.Add(new AsignacionDocenteModel
                        {
                            Id = rdr.GetInt32(0),
                            DocenteNombre = rdr.GetString(1),
                            CursoNombre = rdr.GetString(2),
                            FechaAsignacion = rdr.GetDateTime(3)
                        });
                    }
                }
            }

            BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Ingreso al listado", "Docente");

            return View(lista);
        }

        public ActionResult Crear()
        {
            ViewBag.Docentes = ObtenerDocentes();
            ViewBag.Cursos = ObtenerCursos();
            return View();
        }

        [HttpPost]
        public JsonResult Crear(int docenteId, int cursoId)
        {
            try
            {
                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    string check = "SELECT COUNT(*) FROM DocenteCurso WHERE DocenteId=@d AND CursoId=@c";
                    using (var cmd = new SqlCommand(check, conn))
                    {
                        cmd.Parameters.AddWithValue("@d", docenteId);
                        cmd.Parameters.AddWithValue("@c", cursoId);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                            return Json(new { success = false, message = "Este docente ya está asignado a ese curso." });
                    }

                    string sql = "INSERT INTO DocenteCurso (DocenteId, CursoId) VALUES (@d, @c)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@d", docenteId);
                        cmd.Parameters.AddWithValue("@c", cursoId);
                        cmd.ExecuteNonQuery();
                    }

                    string bit = "INSERT INTO Bitacora (Accion, Fecha, Usuario) VALUES (@a, GETDATE(), @u)";
                    using (var bcmd = new SqlCommand(bit, conn))
                    {
                        bcmd.Parameters.AddWithValue("@a", $"Asignación de docente {docenteId} al curso {cursoId}");
                        bcmd.Parameters.AddWithValue("@u", Session["DocenteActual"] ?? "Administrador");
                        bcmd.ExecuteNonQuery();
                    }
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Agrego docente Id " + docenteId + " al curso Id " + cursoId, "DocenteCurso");

                return Json(new { success = true, message = "Asignación realizada correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Agregar docente Id " + docenteId + " al curso Id " + cursoId, "DocenteCurso");
                return Json(new { success = false, message = "Error al asignar: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            AsignacionDocenteModel asignacion = null;

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();

                string sql = @"SELECT dc.Id, dc.DocenteId, dc.CursoId, d.Usuario AS DocenteNombre, c.Nombre AS CursoNombre
                       FROM DocenteCurso dc
                       INNER JOIN Docentes d ON dc.DocenteId = d.Id
                       INNER JOIN Curso c ON dc.CursoId = c.Id
                       WHERE dc.Id = @id";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            asignacion = new AsignacionDocenteModel
                            {
                                Id = rdr.GetInt32(0),
                                DocenteId = rdr.GetInt32(1),
                                CursoId = rdr.GetInt32(2),
                                DocenteNombre = rdr.GetString(3),
                                CursoNombre = rdr.GetString(4)
                            };
                        }
                    }
                }
            }

            if (asignacion == null)
                return HttpNotFound();

            // Cargar listas
            ViewBag.Docentes = ObtenerDocentes();
            ViewBag.Cursos = ObtenerCursos();

            return View(asignacion);
        }

        [HttpPost]
        public JsonResult Editar(int id, int docenteId, int cursoId)
        {
            try
            {
                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    string check = @"SELECT COUNT(*) FROM DocenteCurso 
                             WHERE DocenteId=@d AND CursoId=@c AND Id<>@id";
                    using (var cmd = new SqlCommand(check, conn))
                    {
                        cmd.Parameters.AddWithValue("@d", docenteId);
                        cmd.Parameters.AddWithValue("@c", cursoId);
                        cmd.Parameters.AddWithValue("@id", id);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                            return Json(new { success = false, message = "Ya existe una asignación igual." });
                    }

                    // Actualizar
                    string sql = "UPDATE DocenteCurso SET DocenteId=@d, CursoId=@c WHERE Id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@d", docenteId);
                        cmd.Parameters.AddWithValue("@c", cursoId);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // Registrar en bitácora
                    string bit = "INSERT INTO Bitacora (Accion, Fecha, Usuario) VALUES (@a, GETDATE(), @u)";
                    using (var bcmd = new SqlCommand(bit, conn))
                    {
                        bcmd.Parameters.AddWithValue("@a", $"Actualización de asignación ID {id}");
                        bcmd.Parameters.AddWithValue("@u", Session["DocenteActual"] ?? "Administrador");
                        bcmd.ExecuteNonQuery();
                    }
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Actualizar docente Id " + docenteId + " al curso Id " + cursoId, "DocenteCurso");

                return Json(new { success = true, message = "Asignación actualizada correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Actualizar docente Id " + docenteId + " al curso Id " + cursoId, "DocenteCurso");
                return Json(new { success = false, message = "Error al actualizar: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    // Verificar que la asignación exista
                    string checkSql = "SELECT COUNT(*) FROM DocenteCurso WHERE Id=@id";
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        int existe = (int)checkCmd.ExecuteScalar();
                        if (existe == 0)
                            return Json(new { success = false, message = "No se encontró la asignación." });
                    }

                    // Eliminar la asignación
                    string deleteSql = "DELETE FROM DocenteCurso WHERE Id=@id";
                    using (var delCmd = new SqlCommand(deleteSql, conn))
                    {
                        delCmd.Parameters.AddWithValue("@id", id);
                        delCmd.ExecuteNonQuery();
                    }

                    // Registrar eliminación en bitácora
                    string bitacoraSql = @"INSERT INTO Bitacora (Accion, Fecha, Usuario)
                                   VALUES (@accion, GETDATE(), @usuario)";
                    using (var bitCmd = new SqlCommand(bitacoraSql, conn))
                    {
                        bitCmd.Parameters.AddWithValue("@accion", $"Eliminación de asignación ID {id}");
                        bitCmd.Parameters.AddWithValue("@usuario", Session["DocenteActual"] ?? "Administrador");
                        bitCmd.ExecuteNonQuery();
                    }
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Eliminada Asignacion Id " + id, "DocenteCurso");

                return Json(new { success = true, message = "Asignación eliminada correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Eliminar Asignacion Id " + id, "DocenteCurso");
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

        private List<SelectListItem> ObtenerDocentes()
        {
            var list = new List<SelectListItem>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Usuario FROM Docentes WHERE Activo=1", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        list.Add(new SelectListItem { Value = rdr["Id"].ToString(), Text = rdr["Usuario"].ToString() });
                }
            }
            return list;
        }

        private List<SelectListItem> ObtenerCursos()
        {
            var list = new List<SelectListItem>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Nombre FROM Curso", conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        list.Add(new SelectListItem { Value = rdr["Id"].ToString(), Text = rdr["Nombre"].ToString() });
                }
            }
            return list;
        }
    }
}
