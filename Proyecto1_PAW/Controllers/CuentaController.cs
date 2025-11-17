using System;
using System.Linq;
using System.Web.Mvc;
using Proyecto1_PAW.Models;
using System.Data.SqlClient;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using Proyecto1_PAW.Utils;

namespace Proyecto1_PAW.Controllers
{
    public class CuentaController : Controller
    {
        private string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        private string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "seguridad.json");

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public JsonResult LoginAjax(string usuario, string contrasena, string tipo)
        {
            // Leer configuración (máximo de intentos)
            var config = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(configPath));
            int maxIntentos = (int)config["MaxIntentos"];

            int intentos = Session["IntentosFallidos"] != null ? (int)Session["IntentosFallidos"] : 0;

            if (intentos >= maxIntentos)
            {
                return Json(new { success = false, message = "Cuenta temporalmente bloqueada. Inténtelo más tarde." });
            }

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();

                // 🔹 Caso DOCENTE
                if (tipo == "docente")
                {
                    string query = @"SELECT Id, Contrasena, Activo 
                             FROM Docentes 
                             WHERE Usuario = @Usuario";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Usuario", usuario);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (!rdr.Read())
                            {
                                Session["IntentosFallidos"] = intentos + 1;
                                return Json(new { success = false, message = "Credenciales inválidas." });
                            }

                            bool activo = rdr.GetBoolean(2);
                            if (!activo)
                                return Json(new { success = false, message = "Cuenta inactiva." });

                            int docenteId = rdr.GetInt32(0);
                            string hashGuardado = rdr.GetString(1);
                            string hashIngresado = EncriptarSHA256(contrasena);

                            if (hashGuardado != hashIngresado)
                            {
                                Session["IntentosFallidos"] = intentos + 1;
                                return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
                            }

                            Session["IntentosFallidos"] = 0;
                            Session["DocenteId"] = docenteId;
                            Session["DocenteActual"] = usuario;

                            rdr.Close();

                            string rolQuery = @"SELECT r.Nombre 
                                        FROM UsuarioRol ur
                                        INNER JOIN Rol r ON ur.RolId = r.Id
                                        WHERE ur.DocenteId = @Id";

                            using (var rolCmd = new SqlCommand(rolQuery, conn))
                            {
                                rolCmd.Parameters.AddWithValue("@Id", docenteId);
                                using (var rd2 = rolCmd.ExecuteReader())
                                {
                                    List<string> roles = new List<string>();
                                    while (rd2.Read())
                                        roles.Add(rd2.GetString(0));

                                    Session["Rol"] = string.Join(",", roles);
                                }
                            }

                            BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Ingreso al sistema", "Cuenta");

                            return Json(new { success = true, redirectUrl = Url.Action("Index", "Panel") });
                        }
                    }
                }

                // 🔹 Caso ESTUDIANTE
                else if (tipo == "estudiante")
                {
                    string query = @"SELECT Id, Contrasena 
                             FROM Estudiante 
                             WHERE Usuario = @Usuario";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Usuario", usuario);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (!rdr.Read())
                            {
                                Session["IntentosFallidos"] = intentos + 1;
                                return Json(new { success = false, message = "Credenciales inválidas." });
                            }

                            int estudianteId = rdr.GetInt32(0);
                            string hashGuardado = rdr.GetString(1);
                            string hashIngresado = EncriptarSHA256(contrasena);

                            if (hashGuardado != hashIngresado)
                            {
                                Session["IntentosFallidos"] = intentos + 1;
                                return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
                            }

                            Session["IntentosFallidos"] = 0;
                            Session["EstudianteId"] = estudianteId;
                            Session["EstudianteActual"] = usuario;
                            Session["Rol"] = "Estudiante";

                            BitacoraHelper.Registrar(Session["EstudianteActual"]?.ToString(), "Ingreso al sistema", "Cuenta");

                            return Json(new { success = true, redirectUrl = Url.Action("Index", "Rendimiento") });
                        }
                    }
                }

                return Json(new { success = false, message = "Tipo de usuario inválido." });
            }
        }


        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        private string EncriptarSHA256(string texto)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}