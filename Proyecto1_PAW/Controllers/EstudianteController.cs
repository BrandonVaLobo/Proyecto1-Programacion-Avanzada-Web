using Proyecto1_PAW.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using Proyecto1_PAW.Utils;

namespace Proyecto1_PAW.Controllers
{
    public class EstudianteController : Controller
    {
        private readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public ActionResult Crear()
        {
            ViewBag.Cuatrimestres = ObtenerCuatrimestres();
            return View();
        }

        [HttpPost]
        public JsonResult CrearAjax(EstudianteDto dto)
        {
            if (dto == null)
                return Json(new { success = false, message = "Datos inválidos." });

            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellidos) ||
                string.IsNullOrWhiteSpace(dto.Identificacion) || string.IsNullOrWhiteSpace(dto.Correo) ||
                dto.FechaNacimiento == default(DateTime) || dto.CuatrimestreId <= 0)
            {
                return Json(new { success = false, message = "Por favor complete todos los campos obligatorios." });
            }

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmdCheck = new SqlCommand(@"SELECT COUNT(1) 
                                                                FROM Estudiante 
                                                                WHERE Identificacion = @Ident 
                                                                OR Correo = @Correo", conn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@Ident", dto.Identificacion.Trim());
                            cmdCheck.Parameters.AddWithValue("@Correo", dto.Correo.Trim());
                            var exists = (int)cmdCheck.ExecuteScalar();
                            if (exists > 0)
                            {
                                tran.Rollback();
                                return Json(new { success = false, message = "Ya existe un estudiante con esa identificación o correo." });
                            }
                        }

                        string insertEst = @"INSERT INTO Estudiante 
                                           (Nombre, Apellidos, Identificacion, FechaNacimiento, Provincia, Canton, Distrito, Correo, CuatrimestreId, Usuario, Contrasena)
                                           VALUES (@Nombre, @Apellidos, @Identificacion, @FechaNacimiento, @Provincia, @Canton, @Distrito, @Correo, @CuatrimestreId, @Usuario, @Contrasena);
                                           SELECT SCOPE_IDENTITY();";

                        int newEstId;
                        string contrasenaEncriptada = EncriptarSHA256(dto.Contrasena.Trim());
                        using (var cmdIns = new SqlCommand(insertEst, conn, tran))
                        {
                            cmdIns.Parameters.AddWithValue("@Nombre", dto.Nombre.Trim());
                            cmdIns.Parameters.AddWithValue("@Apellidos", dto.Apellidos.Trim());
                            cmdIns.Parameters.AddWithValue("@Identificacion", dto.Identificacion.Trim());
                            cmdIns.Parameters.AddWithValue("@FechaNacimiento", dto.FechaNacimiento);
                            cmdIns.Parameters.AddWithValue("@Provincia", dto.Provincia.Trim());
                            cmdIns.Parameters.AddWithValue("@Canton", dto.Canton.Trim());
                            cmdIns.Parameters.AddWithValue("@Distrito", dto.Distrito.Trim());
                            cmdIns.Parameters.AddWithValue("@Correo", dto.Correo.Trim());
                            cmdIns.Parameters.AddWithValue("@CuatrimestreId", dto.CuatrimestreId);
                            cmdIns.Parameters.AddWithValue("@Usuario", dto.Usuario.Trim());
                            cmdIns.Parameters.AddWithValue("@Contrasena", contrasenaEncriptada.Trim());
                            newEstId = Convert.ToInt32(cmdIns.ExecuteScalar());
                        }

                        if (dto.CursosMatriculados != null && dto.CursosMatriculados.Count > 0)
                        {
                            using (var cmdMat = new SqlCommand(@"INSERT INTO Matricula (EstudianteId, CursoId) 
                                                                VALUES (@EstudianteId, @CursoId)", conn, tran))
                            {
                                cmdMat.Parameters.Add("@EstudianteId", SqlDbType.Int).Value = newEstId;
                                cmdMat.Parameters.Add("@CursoId", SqlDbType.Int);
                                foreach (var cursoId in dto.CursosMatriculados)
                                {
                                    cmdMat.Parameters["@CursoId"].Value = cursoId;
                                    cmdMat.ExecuteNonQuery();
                                }
                            }
                        }

                        BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Agrego estudiante identificacion  " + dto.Identificacion, "Estudiante");

                        tran.Commit();
                        return Json(new { success = true, message = "Estudiante registrado correctamente." });
                    }
                    catch (Exception ex)
                    {
                        BitacoraHelper.Registrar(Session["DocenteActual"]?.ToString(), "Error :: Agregar estudiante identificacion  " + dto.Identificacion, "Estudiante");
                        tran.Rollback();
                        return Json(new { success = false, message = "Error al guardar. Intente nuevamente." });
                    }
                }
            }
        }

        [HttpGet]
        public JsonResult ListarCuatrimestres()
        {
            var data = ObtenerCuatrimestres();
            var resultado = data.Select(x => new { Id = x.Key, Nombre = x.Value }).ToList();
            return Json(resultado, JsonRequestBehavior.AllowGet);
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

        public JsonResult CursosPorCuatrimestre(int cuatrimestreId)
        {
            var cursos = new List<object>();
            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"SELECT Id, Nombre 
                                                FROM Curso 
                                                WHERE CuatrimestreId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", cuatrimestreId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            cursos.Add(new { id = rdr.GetInt32(0), nombre = rdr.GetString(1) });
                        }
                    }
                }
            }
            return Json(cursos, JsonRequestBehavior.AllowGet);
        }

        public string EncriptarSHA256(string texto)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(texto);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}