using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Proyecto1_PAW.Utils
{
    public static class BitacoraHelper
    {
        private static readonly string conexion = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;

        public static void Registrar(string usuario, string accion, string modulo)
        {
            try
            {
                string ip = HttpContext.Current?.Request?.UserHostAddress ?? "Desconocida";

                using (var conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    string sql = @"INSERT INTO Bitacora (Accion, Fecha, Usuario, Modulo, IP)
                                   VALUES (@accion, GETDATE(), @usuario, @modulo, @ip)";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@accion", accion);
                        cmd.Parameters.AddWithValue("@usuario", usuario ?? "Desconocido");
                        cmd.Parameters.AddWithValue("@modulo", modulo);
                        cmd.Parameters.AddWithValue("@ip", ip);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }
    }
}