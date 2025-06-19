using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using testautenticacion.Models;
using System.Data.SqlClient;
using System.Data;
using System.Threading;
using Twilio.TwiML.Voice;
using System.Configuration;

namespace testautenticacion.logica
{
	public class LO_Usuario
	{
        // Definir la cadena de conexión a nivel de clase, fuera de métodos
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MiConexion"].ConnectionString;

        public Usuarios EncontrarUsuario(String correo, String clave)
        {
            Usuarios objeto = new Usuarios();

            //using (SqlConnection conexion = new SqlConnection("Data source=DESKTOP-1FFVD4R\\SQLEXPRESS ; Initial Catalog=autentication; Integrated Security=true"))
            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                string query = "SELECT Nombres, Correo, Clave, IdRol, Estado from USUARIOS where Correo = @pcorreo and Clave = @pclave";

                SqlCommand comando = new SqlCommand(query, conexion);
                comando.Parameters.AddWithValue("@pcorreo", correo);
                comando.Parameters.AddWithValue("@pclave", clave);

                conexion.Open(); // Abre la conexión antes de ejecutar el lector

                using (SqlDataReader reader = comando.ExecuteReader()) // Ejecuta el lector
                {
                    while (reader.Read())
                    {
                        objeto = new Usuarios
                        {
                            Nombres = reader["Nombres"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            Clave = reader["Clave"].ToString(),
                            IdRol = (Rol)reader["IdRol"],
                            Estado = Convert.ToBoolean(reader["Estado"])
                        };
                    }
                }
            }
            return objeto;
        }
    }
}