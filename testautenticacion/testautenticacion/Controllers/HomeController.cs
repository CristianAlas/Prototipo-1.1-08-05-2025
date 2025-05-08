using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

using testautenticacion.Permisos;
using testautenticacion.Models;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Rotativa;

namespace testautenticacion.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        //private string connectionString = "Data source=DESKTOP-1FFVD4R\\SQLEXPRESS; Initial Catalog=autentication; Integrated Security=true; MultipleActiveResultSets=True";
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MiConexion"].ConnectionString;
        public ActionResult Index()
        {
            return View();
        }

        [PermisosRol(Rol.Administrador)]
        public ActionResult About()
        {
            ViewBag.Message = "Bienvenido a la pagina About.";

            return View();
        }

        [PermisosRol(Rol.Administrador)]
        public ActionResult Contact()
        {
            ViewBag.Message = "Bienvenido a la pagina Contact.";

            return View();
        }

        public ActionResult CerrarSesion()
        {
            FormsAuthentication.SignOut();

            Session["Usuario"] = null;

            return RedirectToAction("Index", "Acceso");
        }

        [HttpGet]
        public ActionResult Filtro(DateTime? fecha, string recorrido, string docente, string aula)
        {
            List<MonitoreosProgramados> monitoreos = new List<MonitoreosProgramados>();

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                var query = @"SELECT Id, Materia, Docente, Responsable, Aula, HoraInicio, HoraFin, Fecha, Recorrido, Jornada, Ciclo 
                      FROM MonitoreosProgramados WHERE 1=1";
                var parameters = new List<SqlParameter>();

                if (fecha.HasValue)
                {
                    query += " AND CONVERT(date, Fecha) = CONVERT(date, @Fecha)";
                    parameters.Add(new SqlParameter("@Fecha", fecha.Value));
                }

                if (!string.IsNullOrEmpty(docente))
                {
                    query += " AND Docente LIKE @Docente";
                    parameters.Add(new SqlParameter("@Docente", "%" + docente + "%"));
                }

                if (!string.IsNullOrEmpty(aula))
                {
                    query += " AND Aula LIKE @Aula";
                    parameters.Add(new SqlParameter("@Aula", "%" + aula + "%"));
                }

                if (!string.IsNullOrEmpty(recorrido))
                {
                    query += " AND LTRIM(RTRIM(LOWER(Recorrido))) = LOWER(@Recorrido)";
                    parameters.Add(new SqlParameter("@Recorrido", recorrido.Trim().ToLower()));
                }

                query += " ORDER BY Fecha ASC, HoraInicio ASC";

                SqlCommand comando = new SqlCommand(query, conexion);
                foreach (var parameter in parameters)
                {
                    comando.Parameters.Add(parameter);
                }

                conexion.Open();
                using (SqlDataReader reader = comando.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        monitoreos.Add(new MonitoreosProgramados
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Materia = reader["Materia"].ToString(),
                            Docente = reader["Docente"].ToString(),
                            Responsable = reader["Responsable"].ToString(),
                            Aula = reader["Aula"].ToString(),
                            HoraInicio = (TimeSpan)reader["HoraInicio"],
                            HoraFin = (TimeSpan)reader["HoraFin"],
                            Fecha = Convert.ToDateTime(reader["Fecha"]),
                            Recorrido = reader["Recorrido"].ToString(),
                            Jornada = reader["Jornada"].ToString(),
                            Ciclo = reader["Ciclo"].ToString()
                        });
                    }
                }
                // AQUÍ VA EL NUEVO CÓDIGO - después de cargar los monitoreos pero usando la misma conexión
                foreach (var monitoreo in monitoreos)
                {
                    string checkQuery = "SELECT TOP 1 1 FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";
                    SqlCommand cmd = new SqlCommand(checkQuery, conexion);
                    cmd.Parameters.AddWithValue("@Id", monitoreo.Id);
                    monitoreo.TieneMonitoreo = cmd.ExecuteScalar() != null;
                }
            }

            ViewBag.Monitoreos = monitoreos;
            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd");
            ViewBag.RecorridoSeleccionado = recorrido;
            ViewBag.DocenteSeleccionado = docente;
            ViewBag.AulaSeleccionada = aula;

            return View(monitoreos);
        }

        public ActionResult SinPermiso()
        {
            ViewBag.Message = "Usted no cuenta con permisos para ver esta pagina";

            return View();
        }

        [PermisosRol(Rol.Administrador)]
        public ActionResult NuevoMonitoreo()
        {
            return View();
        }

        [PermisosRol(Rol.Administrador)]
        [HttpGet]
        public ActionResult CrearMonitoreo(int id)
        {
            try
            {
                // Verifica si el registro existe
                bool exists = false;
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    string query = "SELECT TOP 1 1 FROM MonitoreosProgramados WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(query, conexion);
                    cmd.Parameters.AddWithValue("@Id", id);
                    conexion.Open();
                    exists = cmd.ExecuteScalar() != null;
                }

                if (!exists)
                {
                    Response.StatusCode = 404;
                    return Content("Monitoreo no encontrado");
                }

                var monitoreo = new RegistrosMonitoreo
                {
                    MonitoreoProgramadoId = id,
                    Fecha = DateTime.Now
                };

                ViewBag.Estados = new List<string> { "Todo bien", "Nadie en el aula", "Cerrado" };
                return PartialView("_FormularioMonitoreo", monitoreo);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Content("Error: " + ex.Message);
            }
        }

        [PermisosRol(Rol.Administrador)]
        [HttpPost]
        public ActionResult GuardarMonitoreo(RegistrosMonitoreo registro)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO RegistrosMonitoreo 
                               (MonitoreoProgramadoId, Fecha, Estado, Comentarios, Feedback) 
                               VALUES (@MonitoreoProgramadoId, @Fecha, @Estado, @Comentarios, @Feedback)";

                    SqlCommand comando = new SqlCommand(query, conexion);
                    comando.Parameters.AddWithValue("@MonitoreoProgramadoId", registro.MonitoreoProgramadoId);
                    comando.Parameters.AddWithValue("@Fecha", registro.Fecha);
                    comando.Parameters.AddWithValue("@Estado", registro.Estado);
                    comando.Parameters.AddWithValue("@Comentarios", registro.Comentarios ?? (object)DBNull.Value);
                    comando.Parameters.AddWithValue("@Feedback", registro.Feedback ?? (object)DBNull.Value);

                    conexion.Open();
                    comando.ExecuteNonQuery();
                }
                return Json(new { success = true });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        // método al HomeController para obtener los datos del monitoreo
        [HttpGet]
        public ActionResult VerMonitoreo(int id)
        {
            try
            {
                MonitoreosProgramados monitoreo = null;
                RegistrosMonitoreo registro = null;

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();

                    // Obtener datos del monitoreo programado
                    string queryMonitoreo = @"SELECT Id, Materia, Docente, Responsable, Aula, HoraInicio, HoraFin, 
                                     Fecha, Recorrido, Jornada, Ciclo 
                                     FROM MonitoreosProgramados WHERE Id = @Id";

                    SqlCommand cmdMonitoreo = new SqlCommand(queryMonitoreo, conexion);
                    cmdMonitoreo.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = cmdMonitoreo.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitoreo = new MonitoreosProgramados
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Materia = reader["Materia"].ToString(),
                                Docente = reader["Docente"].ToString(),
                                Responsable = reader["Responsable"].ToString(),
                                Aula = reader["Aula"].ToString(),
                                HoraInicio = (TimeSpan)reader["HoraInicio"],
                                HoraFin = (TimeSpan)reader["HoraFin"],
                                Fecha = Convert.ToDateTime(reader["Fecha"]),
                                Recorrido = reader["Recorrido"].ToString(),
                                Jornada = reader["Jornada"].ToString(),
                                Ciclo = reader["Ciclo"].ToString()
                            };
                        }
                    }

                    // Obtener datos del registro de monitoreo
                    if (monitoreo != null)
                    {
                        string queryRegistro = @"SELECT Id, MonitoreoProgramadoId, Fecha, Estado, Comentarios, Feedback 
                                       FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";

                        SqlCommand cmdRegistro = new SqlCommand(queryRegistro, conexion);
                        cmdRegistro.Parameters.AddWithValue("@Id", id);

                        using (SqlDataReader reader = cmdRegistro.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                registro = new RegistrosMonitoreo
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    MonitoreoProgramadoId = Convert.ToInt32(reader["MonitoreoProgramadoId"]),
                                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                                    Estado = reader["Estado"].ToString(),
                                    Comentarios = reader["Comentarios"] != DBNull.Value ? reader["Comentarios"].ToString() : null,
                                    Feedback = reader["Feedback"] != DBNull.Value ? reader["Feedback"].ToString() : null,
                                    MonitoreoProgramado = monitoreo
                                };
                            }
                        }
                    }
                }

                if (monitoreo == null || registro == null)
                {
                    Response.StatusCode = 404;
                    return Content("Monitoreo no encontrado");
                }

                ViewBag.Monitoreo = monitoreo;
                return PartialView("_VisualizarMonitoreo", registro);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Content("Error: " + ex.Message);
            }
        }

        [PermisosRol(Rol.Administrador)]
        [HttpGet]
        public ActionResult Reportes(string supervisor, string docente, string aula, DateTime? fecha)
        {
            List<MonitoreosProgramados> monitoreos = new List<MonitoreosProgramados>();

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                var query = @"SELECT mp.Id, mp.Materia, mp.Docente, mp.Responsable, mp.Aula, 
                    mp.HoraInicio, mp.HoraFin, mp.Fecha, mp.Recorrido, mp.Jornada, mp.Ciclo 
                    FROM MonitoreosProgramados mp WHERE 1=1";
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(supervisor))
                {
                    query += " AND mp.Responsable LIKE @Supervisor";
                    parameters.Add(new SqlParameter("@Supervisor", "%" + supervisor + "%"));
                }

                if (!string.IsNullOrEmpty(docente))
                {
                    query += " AND mp.Docente LIKE @Docente";
                    parameters.Add(new SqlParameter("@Docente", "%" + docente + "%"));
                }

                if (!string.IsNullOrEmpty(aula))
                {
                    query += " AND mp.Aula LIKE @Aula";
                    parameters.Add(new SqlParameter("@Aula", "%" + aula + "%"));
                }

                if (fecha.HasValue)
                {
                    query += " AND CONVERT(date, mp.Fecha) = CONVERT(date, @Fecha)";
                    parameters.Add(new SqlParameter("@Fecha", fecha.Value));
                }

                SqlCommand comando = new SqlCommand(query, conexion);
                comando.CommandTimeout = 120;  // Aumenta el tiempo de espera
                foreach (var parameter in parameters)
                {
                    comando.Parameters.Add(parameter);
                }

                conexion.Open();
                using (SqlDataReader reader = comando.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        monitoreos.Add(new MonitoreosProgramados
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Materia = reader["Materia"].ToString(),
                            Docente = reader["Docente"].ToString(),
                            Responsable = reader["Responsable"].ToString(),
                            Aula = reader["Aula"].ToString(),
                            HoraInicio = (TimeSpan)reader["HoraInicio"],
                            HoraFin = (TimeSpan)reader["HoraFin"],
                            Fecha = Convert.ToDateTime(reader["Fecha"]),
                            Recorrido = reader["Recorrido"].ToString(),
                            Jornada = reader["Jornada"].ToString(),
                            Ciclo = reader["Ciclo"].ToString()
                        });
                    }
                }

                // Check if there's a monitoring record for each scheduled monitoring
                foreach (var monitoreo in monitoreos)
                {
                    string checkQuery = "SELECT TOP 1 1 FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";
                    SqlCommand cmd = new SqlCommand(checkQuery, conexion);
                    cmd.Parameters.AddWithValue("@Id", monitoreo.Id);
                    monitoreo.TieneMonitoreo = cmd.ExecuteScalar() != null;
                }
            }

            ViewBag.SupervisorSeleccionado = supervisor;
            ViewBag.DocenteSeleccionado = docente;
            ViewBag.AulaSeleccionada = aula;
            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd");

            return View(monitoreos);
        }

        [HttpGet]
        public ActionResult DetalleMonitoreo(int id)
        {
            MonitoreosProgramados monitoreo = null;
            RegistrosMonitoreo registro = null;

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                conexion.Open();

                // Get scheduled monitoring data
                string queryMonitoreo = @"SELECT Id, Materia, Docente, Responsable, Aula, HoraInicio, HoraFin, 
                           Fecha, Recorrido, Jornada, Ciclo 
                           FROM MonitoreosProgramados WHERE Id = @Id";

                SqlCommand cmdMonitoreo = new SqlCommand(queryMonitoreo, conexion);
                cmdMonitoreo.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader reader = cmdMonitoreo.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        monitoreo = new MonitoreosProgramados
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Materia = reader["Materia"].ToString(),
                            Docente = reader["Docente"].ToString(),
                            Responsable = reader["Responsable"].ToString(),
                            Aula = reader["Aula"].ToString(),
                            HoraInicio = (TimeSpan)reader["HoraInicio"],
                            HoraFin = (TimeSpan)reader["HoraFin"],
                            Fecha = Convert.ToDateTime(reader["Fecha"]),
                            Recorrido = reader["Recorrido"].ToString(),
                            Jornada = reader["Jornada"].ToString(),
                            Ciclo = reader["Ciclo"].ToString()
                        };
                    }
                }

                // Get monitoring registration data
                if (monitoreo != null)
                {
                    string queryRegistro = @"SELECT Id, MonitoreoProgramadoId, Fecha, Estado, Comentarios, Feedback 
                             FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";

                    SqlCommand cmdRegistro = new SqlCommand(queryRegistro, conexion);
                    cmdRegistro.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = cmdRegistro.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            registro = new RegistrosMonitoreo
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                MonitoreoProgramadoId = Convert.ToInt32(reader["MonitoreoProgramadoId"]),
                                Fecha = Convert.ToDateTime(reader["Fecha"]),
                                Estado = reader["Estado"].ToString(),
                                Comentarios = reader["Comentarios"] != DBNull.Value ? reader["Comentarios"].ToString() : null,
                                Feedback = reader["Feedback"] != DBNull.Value ? reader["Feedback"].ToString() : null,
                                MonitoreoProgramado = monitoreo
                            };
                        }
                    }
                }
            }

            if (monitoreo == null)
            {
                return Json(new { success = false, message = "Monitoreo no encontrado" }, JsonRequestBehavior.AllowGet);
            }

            if (registro == null)
            {
                // If no registration exists, return only the monitoring schedule data
                return Json(new
                {
                    success = true,
                    tieneRegistro = false,
                    monitoreo = new
                    {
                        id = monitoreo.Id,
                        responsable = monitoreo.Responsable,
                        horaMonitoreo = monitoreo.HoraInicio.ToString(@"hh\:mm") + " - " + monitoreo.HoraFin.ToString(@"hh\:mm"),
                        materia = monitoreo.Materia,
                        recorrido = monitoreo.Recorrido,
                        docente = monitoreo.Docente,
                        jornada = monitoreo.Jornada,
                        aula = monitoreo.Aula,
                        ciclo = monitoreo.Ciclo,
                        fecha = monitoreo.Fecha.ToString("dd/MM/yyyy")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Return both monitoring schedule and registration data
                return Json(new
                {
                    success = true,
                    tieneRegistro = true,
                    monitoreo = new
                    {
                        id = monitoreo.Id,
                        responsable = monitoreo.Responsable,
                        horaMonitoreo = monitoreo.HoraInicio.ToString(@"hh\:mm") + " - " + monitoreo.HoraFin.ToString(@"hh\:mm"),
                        materia = monitoreo.Materia,
                        recorrido = monitoreo.Recorrido,
                        docente = monitoreo.Docente,
                        jornada = monitoreo.Jornada,
                        aula = monitoreo.Aula,
                        ciclo = monitoreo.Ciclo,
                        fecha = monitoreo.Fecha.ToString("dd/MM/yyyy")
                    },
                    registro = new
                    {
                        id = registro.Id,
                        fechaRegistro = registro.Fecha.ToString("dd/MM/yyyy HH:mm"),
                        estado = registro.Estado,
                        comentarios = registro.Comentarios,
                        feedback = registro.Feedback
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [PermisosRol(Rol.Administrador)]
        public ActionResult GenerarPDF(string supervisor, string docente, string aula, DateTime? fecha)
        {
            var modelo = new ReportePDFViewModel
            {
                Monitoreos = new List<MonitoreosProgramados>(),
                Registros = new List<RegistrosMonitoreo>()
            };

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                var queryMonitoreos = @"SELECT mp.Id, mp.Materia, mp.Docente, mp.Responsable, mp.Aula, mp.Fecha 
                                FROM MonitoreosProgramados mp WHERE 1=1";
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(docente))
                {
                    queryMonitoreos += " AND mp.Docente LIKE @Docente";
                    parameters.Add(new SqlParameter("@Docente", "%" + docente + "%"));
                }

                if (!string.IsNullOrEmpty(aula))
                {
                    queryMonitoreos += " AND mp.Aula LIKE @Aula";
                    parameters.Add(new SqlParameter("@Aula", "%" + aula + "%"));
                }

                if (fecha.HasValue)
                {
                    queryMonitoreos += " AND CONVERT(date, mp.Fecha) = CONVERT(date, @Fecha)";
                    parameters.Add(new SqlParameter("@Fecha", fecha.Value));
                }

                SqlCommand comandoMonitoreos = new SqlCommand(queryMonitoreos, conexion);
                comandoMonitoreos.CommandTimeout = 120;
                foreach (var parameter in parameters)
                {
                    comandoMonitoreos.Parameters.Add(parameter);
                }

                conexion.Open();
                using (SqlDataReader reader = comandoMonitoreos.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var monitoreo = new MonitoreosProgramados
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Materia = reader["Materia"].ToString(),
                            Docente = reader["Docente"].ToString(),
                            Responsable = reader["Responsable"].ToString(),
                            Aula = reader["Aula"].ToString(),
                            Fecha = Convert.ToDateTime(reader["Fecha"])
                        };

                        modelo.Monitoreos.Add(monitoreo);

                        // Obtener registros para cada monitoreo
                        var queryRegistros = @"SELECT Id, MonitoreoProgramadoId, Fecha, Estado, Comentarios, Feedback 
                                       FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";
                        using (SqlCommand comandoRegistros = new SqlCommand(queryRegistros, conexion))
                        {
                            comandoRegistros.Parameters.AddWithValue("@Id", monitoreo.Id);
                            using (SqlDataReader readerRegistros = comandoRegistros.ExecuteReader())
                            {
                                while (readerRegistros.Read())
                                {
                                    var registro = new RegistrosMonitoreo
                                    {
                                        Id = Convert.ToInt32(readerRegistros["Id"]),
                                        MonitoreoProgramadoId = Convert.ToInt32(readerRegistros["MonitoreoProgramadoId"]),
                                        Fecha = Convert.ToDateTime(readerRegistros["Fecha"]),
                                        Estado = readerRegistros["Estado"].ToString(),
                                        Comentarios = readerRegistros["Comentarios"] != DBNull.Value ? readerRegistros["Comentarios"].ToString() : null,
                                        Feedback = readerRegistros["Feedback"] != DBNull.Value ? readerRegistros["Feedback"].ToString() : null
                                    };

                                    modelo.Registros.Add(registro);
                                }
                            }
                        }
                    }
                }
            }

            return new ViewAsPdf("_ReportePDF", modelo)
            {
                FileName = "Reporte_Monitoreos.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageMargins = new Rotativa.Options.Margins(10, 10, 10, 10)
            };
        }

        [PermisosRol(Rol.Administrador)]
        [HttpPost]
        public ActionResult GuardarReporte(string nombreArchivo, string datosReporte)
        {
            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO ReportesGenerados (NombreArchivo, DatosReporte) VALUES (@Nombre, @Datos)";
                SqlCommand comando = new SqlCommand(query, conexion);
                comando.Parameters.AddWithValue("@Nombre", nombreArchivo);
                comando.Parameters.AddWithValue("@Datos", datosReporte);

                conexion.Open();
                comando.ExecuteNonQuery();
            }

            return Json(new { success = true });
        }


    }
}