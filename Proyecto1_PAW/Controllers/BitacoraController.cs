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
    public class BitacoraController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Index(string usuario = "0000", string modulo = "0000", string accion = "0000", string fecha = "0000", int page = 1, int pageSize = 10)
        {
            // 3️⃣ Solo administradores pueden acceder
            if (Session["Rol"] == null || !Session["Rol"].ToString().Contains("Administrador"))
                return RedirectToAction("AccesoDenegado", "Cuenta");

            if (fecha == "0000")
            {
                fecha = DateTime.Now.ToString("yyyy-MM-dd");
            }

            var lista = new List<BitacoraModel>();

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                string sql = @"SELECT Id, Usuario, Accion, Modulo, Fecha, IP 
                               FROM Bitacora
                               WHERE 
                                    (Usuario LIKE IIF(@usuario IN ('', '0000'), '%', '%' + @usuario + '%'))
                                AND (Modulo LIKE IIF(@modulo IN ('', '0000'), '%', '%' + @modulo + '%'))
                                AND (Accion LIKE IIF(@accion IN ('', '0000'), '%', '%' + @accion + '%'))
                                AND (CONVERT(date, Fecha) = IIF(@fecha IN ('', '0000'), CONVERT(date, Fecha), @fecha))
                               ORDER BY Fecha DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@modulo", modulo);
                    cmd.Parameters.AddWithValue("@accion", accion);
                    cmd.Parameters.AddWithValue("@fecha", string.IsNullOrEmpty(fecha) ? (object)DBNull.Value : fecha);

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lista.Add(new BitacoraModel
                            {
                                Id = rdr.GetInt32(0),
                                Usuario = rdr.IsDBNull(1) ? "N/A" : rdr.GetString(1),
                                Accion = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                                Modulo = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                                Fecha = rdr.GetDateTime(4).ToString("dd/MM/yyyy HH:mm:ss"),
                                IP = rdr.IsDBNull(5) ? "N/A" : rdr.GetString(5)
                            });
                        }
                    }
                }
            }

            // Paginación
            int totalRegistros = lista.Count;
            var registros = lista.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRegistros / pageSize);
            ViewBag.Usuario = usuario;
            ViewBag.Modulo = modulo;
            ViewBag.Accion = accion;
            ViewBag.Fecha = fecha;

            return View(registros);
        }
    }
}