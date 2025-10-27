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
        public JsonResult LoginAjax(string usuario, string contrasena)
        {
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
                string query = @"SELECT Contrasena 
                                FROM Docentes 
                                WHERE Usuario = @Usuario 
                                AND Activo = 1";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Usuario", usuario);
                    var result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        Session["IntentosFallidos"] = intentos + 1;
                        return Json(new { success = false, message = "Credenciales inválidas." });
                    }

                    string hashGuardado = result.ToString();
                    string hashIngresado = EncriptarSHA256(contrasena);

                    if (hashGuardado != hashIngresado)
                    {
                        Session["IntentosFallidos"] = intentos + 1;
                        return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
                    }

                    Session["IntentosFallidos"] = 0;
                    Session["DocenteActual"] = usuario;
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Panel") });
                }
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