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
    public class RolController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["DocenteActual"] == null)
                return RedirectToAction("Login", "Cuenta");

            var lista = new List<RolModel>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT Id, Nombre, Descripcion FROM Rol", conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        lista.Add(new RolModel
                        {
                            Id = rdr.GetInt32(0),
                            Nombre = rdr.GetString(1),
                            Descripcion = rdr.IsDBNull(2) ? "" : rdr.GetString(2)
                        });
                    }
                }
            }

            BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Ingreso al listado", "Rol");

            return View(lista);
        }

        [HttpPost]
        public JsonResult Crear(string nombre, string descripcion)
        {
            try
            {
                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();
                    string sql = "INSERT INTO Rol (Nombre, Descripcion) VALUES (@n, @d)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n", nombre);
                        cmd.Parameters.AddWithValue("@d", (object)descripcion ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Creación del rol " + nombre, "Rol");

                return Json(new { success = true, message = "Rol creado correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Creación del rol " + nombre, "Rol");
                return Json(new { success = false, message = "Error al crear rol: " + ex.Message });
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

                    string rolNombre = "";
                    using (var cmdRol = new SqlCommand("SELECT Nombre FROM Rol WHERE Id=@id", conn))
                    {
                        cmdRol.Parameters.AddWithValue("@id", id);
                        var result = cmdRol.ExecuteScalar();
                        rolNombre = result != null ? result.ToString() : "(desconocido)";
                    }

                    string sql = "DELETE FROM Rol WHERE Id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    };
                }

                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Eliminacion del rol Id " + id, "Rol");

                return Json(new { success = true, message = "Rol eliminado correctamente." });
            }
            catch (Exception ex)
            {
                BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Eliminacion del rol Id " + id, "Rol");
                return Json(new { success = false, message = "No se pudo eliminar el rol: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Asignar(int id)
        {
            if (Session["Rol"] == null || !Session["Rol"].ToString().Contains("Administrador"))
                return RedirectToAction("AccesoDenegado", "Cuenta");

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();

                // Obtener docente
                var docenteCmd = new SqlCommand("SELECT Usuario FROM Docentes WHERE Id=@id", conn);
                docenteCmd.Parameters.AddWithValue("@id", id);
                var nombre = (string)docenteCmd.ExecuteScalar();

                // Obtener roles
                var roles = new List<RolModel>();
                var rolCmd = new SqlCommand("SELECT Id, Nombre FROM Rol", conn);
                using (var rdr = rolCmd.ExecuteReader())
                {
                    while (rdr.Read())
                        roles.Add(new RolModel { Id = rdr.GetInt32(0), Nombre = rdr.GetString(1) });
                }

                // Obtener roles actuales del docente
                var asignados = new List<int>();
                var asignadoCmd = new SqlCommand("SELECT RolId FROM UsuarioRol WHERE DocenteId=@id", conn);
                asignadoCmd.Parameters.AddWithValue("@id", id);
                using (var rdr2 = asignadoCmd.ExecuteReader())
                {
                    while (rdr2.Read())
                        asignados.Add(rdr2.GetInt32(0));
                }

                ViewBag.DocenteId = id;
                ViewBag.DocenteNombre = nombre;
                ViewBag.Asignados = asignados;
                return View(roles);
            }
        }

        [HttpPost]
        public ActionResult GuardarRoles(int docenteId, int[] roles)
        {
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();

                var delete = new SqlCommand("DELETE FROM UsuarioRol WHERE DocenteId=@id", conn);
                delete.Parameters.AddWithValue("@id", docenteId);
                delete.ExecuteNonQuery();

                foreach (var rol in roles)
                {
                    var insert = new SqlCommand("INSERT INTO UsuarioRol (DocenteId, RolId) VALUES (@d,@r)", conn);
                    insert.Parameters.AddWithValue("@d", docenteId);
                    insert.Parameters.AddWithValue("@r", rol);
                    insert.ExecuteNonQuery();
                }

            }

            BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Asignación de roles al docente Id " + docenteId, "Rol");

            return RedirectToAction("Index");
        }

        public ActionResult ListaUsuarios()
        {
            if (Session["Rol"] == null || !Session["Rol"].ToString().Contains("Administrador"))
                return RedirectToAction("AccesoDenegado", "Cuenta");

            var lista = new List<UsuarioRolViewModel>();

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();

                string sql = @"SELECT d.Id, d.Usuario, 
                              ISNULL(STUFF((
                                  SELECT ', ' + r.Nombre
                                  FROM UsuarioRol ur
                                  INNER JOIN Rol r ON ur.RolId = r.Id
                                  WHERE ur.DocenteId = d.Id
                                  FOR XML PATH('')), 1, 2, ''), '-') AS Roles
                       FROM Docentes d";

                using (var cmd = new SqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        lista.Add(new UsuarioRolViewModel
                        {
                            DocenteId = rdr.GetInt32(0),
                            Nombre = rdr.GetString(1),
                            Roles = rdr.GetString(2)
                        });
                    }
                }
            }

            BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Ingreso a la Lista de Usuarios", "Rol");

            return View(lista);
        }
    }
}