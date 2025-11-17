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
    public class CursoController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Rol"] == null || !Session["Rol"].ToString().Contains("Administrador"))
                return RedirectToAction("AccesoDenegado", "Cuenta");

            var lista = new List<CursoModel>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT c.Id, c.Codigo, c.Nombre, c.Descripcion, c.Creditos, 
                                      cu.Nombre AS Cuatrimestre, c.FechaCreacion, c.CreadoPor
                               FROM Curso c
                               INNER JOIN Cuatrimestre cu ON cu.Id = c.CuatrimestreId
                               ORDER BY c.Id DESC";

                using (var cmd = new SqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        lista.Add(new CursoModel
                        {
                            Id = rdr.GetInt32(0),
                            Codigo = rdr.GetString(1),
                            Nombre = rdr.GetString(2),
                            Descripcion = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                            Creditos = rdr.GetInt32(4),
                            CuatrimestreNombre = rdr.GetString(5),
                            FechaCreacion = rdr.GetDateTime(6),
                            CreadoPor = rdr.IsDBNull(7) ? "" : rdr.GetString(7)
                        });
                    }
                }
            }

            BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Ingreso al listado", "Curso");

            return View(lista);
        }

        [HttpPost]
        public JsonResult Crear(string codigo, string nombre, string descripcion, int creditos, int cuatrimestreId)
        {
            try
            {
                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    string check = "SELECT COUNT(*) FROM Curso WHERE Codigo = @Codigo";
                    using (var checkCmd = new SqlCommand(check, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Codigo", codigo);
                        int existe = (int)checkCmd.ExecuteScalar();
                        if (existe > 0)
                            return Json(new { success = false, message = "Ya existe un curso con ese código." });
                    }

                    string sql = @"INSERT INTO Curso (Codigo, Nombre, Descripcion, Creditos, CuatrimestreId, FechaCreacion, CreadoPor)
                                   VALUES (@Codigo, @Nombre, @Descripcion, @Creditos, @CuatrimestreId, GETDATE(), @CreadoPor)";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@Descripcion", (object)descripcion ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Creditos", creditos);
                        cmd.Parameters.AddWithValue("@CuatrimestreId", cuatrimestreId);
                        cmd.Parameters.AddWithValue("@CreadoPor", Session["DocenteActual"] ?? "Admin");
                        cmd.ExecuteNonQuery();
                    }

                    string bitacora = @"INSERT INTO Bitacora (Accion, Fecha, Usuario)
                                        VALUES (@Accion, GETDATE(), @Usuario)";
                    using (var bitCmd = new SqlCommand(bitacora, conn))
                    {
                        bitCmd.Parameters.AddWithValue("@Accion", $"Creación de curso '{nombre}' ({codigo})");
                        bitCmd.Parameters.AddWithValue("@Usuario", Session["DocenteActual"] ?? "Admin");
                        bitCmd.ExecuteNonQuery();
                    }

                    BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Agrego curso codigo " + codigo, "Curso");

                }

                return Json(new { success = true, message = "Curso creado correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Crear curso codigo " + codigo, "Curso");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            if (Session["Rol"] == null || !Session["Rol"].ToString().Contains("Administrador"))
                return RedirectToAction("AccesoDenegado", "Cuenta");

            CursoModel curso = null;
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT Id, Codigo, Nombre, Descripcion, Creditos, CuatrimestreId 
                       FROM Curso WHERE Id=@id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            curso = new CursoModel
                            {
                                Id = rdr.GetInt32(0),
                                Codigo = rdr.GetString(1),
                                Nombre = rdr.GetString(2),
                                Descripcion = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                                Creditos = rdr.GetInt32(4),
                                CuatrimestreId = rdr.GetInt32(5)
                            };
                        }
                    }
                }
            }

            ViewBag.Cuatrimestres = ObtenerCuatrimestres();
            return View(curso);
        }

        [HttpPost]
        public JsonResult GuardarEdicion(int id, string nombre, string descripcion, int creditos, int cuatrimestreId)
        {
            try
            {
                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    string check = @"SELECT COUNT(*) 
                             FROM Matricula 
                             WHERE CursoId=@id";
                    int inscritos;
                    using (var cmdCheck = new SqlCommand(check, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@id", id);
                        inscritos = (int)cmdCheck.ExecuteScalar();
                    }

                    string sql = @"UPDATE Curso 
                           SET Nombre=@n, Descripcion=@d, Creditos=@c, CuatrimestreId=@cu,
                               FechaModificacion=GETDATE(), ModificadoPor=@u
                           WHERE Id=@id";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n", nombre);
                        cmd.Parameters.AddWithValue("@d", (object)descripcion ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@c", creditos);
                        cmd.Parameters.AddWithValue("@cu", cuatrimestreId);
                        cmd.Parameters.AddWithValue("@u", Session["DocenteActual"] ?? "Admin");
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    string bitacora = @"INSERT INTO Bitacora (Accion, Fecha, Usuario)
                                VALUES (@a, GETDATE(), @u)";
                    using (var bitCmd = new SqlCommand(bitacora, conn))
                    {
                        string accion = inscritos > 0
                            ? $"Edición de curso ID {id} (sin cambio de código, tiene {inscritos} inscritos)"
                            : $"Edición de curso ID {id}";
                        bitCmd.Parameters.AddWithValue("@a", accion);
                        bitCmd.Parameters.AddWithValue("@u", Session["DocenteActual"] ?? "Admin");
                        bitCmd.ExecuteNonQuery();
                    }
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Edito curso Id " + id, "Curso");

                return Json(new { success = true, message = "Curso actualizado correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Editar curso Id " + id, "Curso");
                return Json(new { success = false, message = "Error: " + ex.Message });
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

                    string checkSql = @"SELECT COUNT(*) FROM Matricula WHERE CursoId = @id";
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            return Json(new { success = false, message = "No se puede eliminar el curso porque tiene estudiantes inscritos." });
                        }
                    }

                    string deleteSql = @"DELETE FROM Curso WHERE Id = @id";
                    using (var deleteCmd = new SqlCommand(deleteSql, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", id);
                        deleteCmd.ExecuteNonQuery();
                    }

                    string bitacoraSql = @"INSERT INTO Bitacora (Accion, Fecha, Usuario)
                                   VALUES (@accion, GETDATE(), @usuario)";
                    using (var bitCmd = new SqlCommand(bitacoraSql, conn))
                    {
                        bitCmd.Parameters.AddWithValue("@accion", $"Eliminación del curso ID {id}");
                        bitCmd.Parameters.AddWithValue("@usuario", Session["DocenteActual"] ?? "Desconocido");
                        bitCmd.ExecuteNonQuery();
                    }
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Elimino curso Id " + id, "Curso");

                return Json(new { success = true, message = "Curso eliminado correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Eliminar curso Id " + id, "Curso");
                return Json(new { success = false, message = "Error al eliminar el curso: " + ex.Message });
            }
        }

        private List<KeyValuePair<int, string>> ObtenerCuatrimestres()
        {
            var list = new List<KeyValuePair<int, string>>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"SELECT Id, Nombre 
                                                FROM Cuatrimestre 
                                                ORDER BY Id", conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new KeyValuePair<int, string>(rdr.GetInt32(0), rdr.GetString(1)));
                    }
                }
            }
            return list;
        }
    }
}