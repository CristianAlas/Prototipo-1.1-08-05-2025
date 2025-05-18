using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace testautenticacion.logica
{
    public class COD_Emergencia
    {
        public void GuardarEmergencia(string nombreMonitor, string edificio, DateTime fechaHora, string comentarios)
        {
            try
            {
                if (string.IsNullOrEmpty(edificio))
                    throw new Exception("El edificio no puede ser nulo o vacío.");

                if (comentarios == null)
                    comentarios = "Sin comentarios"; // Asignar valor por defecto

                string connectionString = ConfigurationManager.ConnectionStrings["MiConexion"].ConnectionString;

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Emergencias (NombreMonitor, Edificio, FechaHora, Comentarios) " +
                                   "VALUES (@NombreMonitor, @Edificio, @FechaHora, @Comentarios)";

                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        comando.Parameters.AddWithValue("@NombreMonitor", nombreMonitor);
                        comando.Parameters.AddWithValue("@Edificio", edificio);
                        comando.Parameters.AddWithValue("@FechaHora", fechaHora);
                        comando.Parameters.AddWithValue("@Comentarios", comentarios);

                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al guardar emergencia: " + ex.Message);
            }
        }
    }
}