﻿using System;
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
using System.Net;
using System.Text;
using testautenticacion.logica;

namespace testautenticacion.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MiConexion"].ConnectionString;
        /*
        public ActionResult Index()
        {

            // Obtener total de reportes en PDF
            int totalReportes = 0;
            int monitoreosCompletados = 0;
            int monitoreosPendientes = 0;
            int incidentesCriticos = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Reportes generados                
                using (SqlCommand cmd = new SqlCommand("SELECT TotalPDF, TotalCSV FROM ReporteContador WHERE Id = 1", conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        ViewBag.TotalPDF = reader.GetInt32(reader.GetOrdinal("TotalPDF"));
                        ViewBag.TotalCSV = reader.GetInt32(reader.GetOrdinal("TotalCSV"));
                    }
                }
                //Contador de emergencias
                using (SqlCommand cmd1 = new SqlCommand("SELECT Count (*) FROM Emergencias", conn))
                {
                    incidentesCriticos = (int)cmd1.ExecuteScalar();
                }
                //Contador de Monitoreos completados y Monitoreos pendientes usando SP
                using (SqlCommand cmd2 = new SqlCommand("ObtenerConteoMonitoreos", conn))
                {
                    cmd2.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd2.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitoreosCompletados = reader.GetInt32(reader.GetOrdinal("MonitoreosCompletados"));
                            monitoreosPendientes = reader.GetInt32(reader.GetOrdinal("MonitoreosPendientes"));
                        }
                    }
                }
            }

            ViewBag.TotalReportesGenerados = totalReportes;
            ViewBag.MonitoreosCompletados = monitoreosCompletados;
            ViewBag.MonitoreosPendientes = monitoreosPendientes;
            ViewBag.IncidentesCriticos = incidentesCriticos;

            // Obtener usuario de la sesión y crear el modelo
            var usuario = Session["Usuario"] as Usuarios;

            var modelo = new Emergencia
            {
                NombreMonitor = usuario != null ? usuario.Nombres : "",
                FechaHora = DateTime.Now
            };

            if (usuario != null)
            {
                ViewBag.NombreUsuario = usuario.Nombres;
            }

            return View(modelo);
        }
        */
        public ActionResult Index()
        {
            int totalReportes = 0;
            int monitoreosCompletados = 0;
            int monitoreosPendientes = 0;
            int incidentesCriticos = 0;
            var listaEmergencias = new List<Emergencia>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Reportes generados                
                using (SqlCommand cmd = new SqlCommand("SELECT TotalPDF, TotalCSV FROM ReporteContador WHERE Id = 1", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewBag.TotalPDF = reader.GetInt32(reader.GetOrdinal("TotalPDF"));
                            ViewBag.TotalCSV = reader.GetInt32(reader.GetOrdinal("TotalCSV"));
                        }
                    }
                }

                //Contador de emergencias
                using (SqlCommand cmd1 = new SqlCommand("SELECT Count (*) FROM Emergencias", conn))
                {
                    incidentesCriticos = (int)cmd1.ExecuteScalar();
                }

                //Conteo de monitoreos
                using (SqlCommand cmd2 = new SqlCommand("ObtenerConteoMonitoreos", conn))
                {
                    cmd2.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader reader = cmd2.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitoreosCompletados = reader.GetInt32(reader.GetOrdinal("MonitoreosCompletados"));
                            monitoreosPendientes = reader.GetInt32(reader.GetOrdinal("MonitoreosPendientes"));
                        }
                    }
                }

                // Lista de emergencias
                using (SqlCommand cmd3 = new SqlCommand("SELECT Id, NombreMonitor, Edificio, FechaHora, Comentarios FROM Emergencias", conn))
                {
                    using (SqlDataReader reader = cmd3.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaEmergencias.Add(new Emergencia
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NombreMonitor = reader["NombreMonitor"].ToString(),
                                Edificio = reader["Edificio"].ToString(),
                                FechaHora = Convert.ToDateTime(reader["FechaHora"]),
                                Comentarios = reader["Comentarios"].ToString()
                            });
                        }
                    }
                }
            }

            // ViewBags
            ViewBag.TotalReportesGenerados = totalReportes;
            ViewBag.MonitoreosCompletados = monitoreosCompletados;
            ViewBag.MonitoreosPendientes = monitoreosPendientes;
            ViewBag.IncidentesCriticos = incidentesCriticos;
            ViewBag.ListaEmergencias = listaEmergencias;

            // Usuario
            var usuario = Session["Usuario"] as Usuarios;
            var modelo = new Emergencia
            {
                NombreMonitor = usuario?.Nombres ?? "",
                FechaHora = DateTime.Now
            };
            if (usuario != null)
            {
                ViewBag.NombreUsuario = usuario.Nombres;
            }

            return View(modelo);
        }
        [PermisosRol(Rol.Administrador | Rol.Monitor | Rol.Coordinador)]
        public ActionResult About()
        {
            ViewBag.Message = "Bienvenido a la pagina About.";

            return View();
        }

        [PermisosRol(Rol.Administrador | Rol.Coordinador)]
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
        public ActionResult Filtro(DateTime? fecha, string recorrido, string docente, string aula, string estado)
        {
            List<MonitoreosProgramados> monitoreos = new List<MonitoreosProgramados>();

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                SqlCommand comando = new SqlCommand("sp_ObtenerMonitoreosProgramadosConFiltros", conexion);
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.AddWithValue("@Fecha", fecha ?? (object)DBNull.Value);
                comando.Parameters.AddWithValue("@Recorrido", string.IsNullOrWhiteSpace(recorrido) ? (object)DBNull.Value : recorrido.Trim().ToLower());
                comando.Parameters.AddWithValue("@Docente", string.IsNullOrWhiteSpace(docente) ? (object)DBNull.Value : docente);
                comando.Parameters.AddWithValue("@Aula", string.IsNullOrWhiteSpace(aula) ? (object)DBNull.Value : aula);

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

                // Segunda parte: verificar si tiene monitoreo en la tabla RegistrosMonitoreo
                foreach (var monitoreo in monitoreos)
                {
                    SqlCommand checkCmd = new SqlCommand("SELECT TOP 1 1 FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id", conexion);
                    checkCmd.Parameters.AddWithValue("@Id", monitoreo.Id);
                    object result = checkCmd.ExecuteScalar();
                    monitoreo.TieneMonitoreo = result != null;

                    string estadoActual = "Pendiente";

                    if (monitoreo.TieneMonitoreo)
                    {
                        estadoActual = "Completado";
                    }
                    else if (monitoreo.Fecha.Date < DateTime.Today)
                    {
                        estadoActual = "Expirado";
                    }

                    monitoreo.EstadoMonitoreo = estadoActual;

                    // Guardar el estado actualizado en la base de datos
                    SqlCommand updateCmd = new SqlCommand("UPDATE MonitoreosProgramados SET EstadoMonitoreo = @Estado WHERE Id = @Id", conexion);
                    updateCmd.Parameters.AddWithValue("@Estado", estadoActual);
                    updateCmd.Parameters.AddWithValue("@Id", monitoreo.Id);
                    updateCmd.ExecuteNonQuery();
                }
                if (!string.IsNullOrWhiteSpace(estado))
                {
                    monitoreos = monitoreos.Where(m => m.EstadoMonitoreo == estado).ToList();
                }

            }

            ViewBag.Monitoreos = monitoreos;
            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd");
            ViewBag.RecorridoSeleccionado = recorrido;
            ViewBag.DocenteSeleccionado = docente;
            ViewBag.AulaSeleccionada = aula;
            ViewBag.EstadoSeleccionado = estado;

            ViewBag.Usuario = Session["Usuario"];

            return View(monitoreos);
        }

        public ActionResult SinPermiso()
        {
            ViewBag.Message = "Usted no cuenta con permisos para ver esta pagina";

            return View();
        }

        //[PermisosRol(Rol.Administrador)]
        public ActionResult NuevoMonitoreo()
        {
            return View();
        }

        //[PermisosRol(Rol.Administrador)]
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

        //[PermisosRol(Rol.Administrador)]
        [HttpPost]
        public ActionResult GuardarMonitoreo(RegistrosMonitoreo registro)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    SqlCommand comando = new SqlCommand("sp_GuardarMonitoreo", conexion);
                    comando.CommandType = CommandType.StoredProcedure;

                    comando.Parameters.AddWithValue("@MonitoreoProgramadoId", registro.MonitoreoProgramadoId);
                    comando.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    comando.Parameters.AddWithValue("@Estado", registro.Estado);
                    comando.Parameters.AddWithValue("@Comentarios", string.IsNullOrEmpty(registro.Comentarios) ? (object)DBNull.Value : registro.Comentarios);
                    comando.Parameters.AddWithValue("@Feedback", string.IsNullOrEmpty(registro.Feedback) ? (object)DBNull.Value : registro.Feedback);

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

                    SqlCommand cmd = new SqlCommand("sp_ObtenerMonitoreoConRegistro", conexion);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitoreo = new MonitoreosProgramados
                            {
                                Id = Convert.ToInt32(reader["MonitoreoId"]),
                                Materia = reader["Materia"].ToString(),
                                Docente = reader["Docente"].ToString(),
                                Responsable = reader["Responsable"].ToString(),
                                Aula = reader["Aula"].ToString(),
                                HoraInicio = (TimeSpan)reader["HoraInicio"],
                                HoraFin = (TimeSpan)reader["HoraFin"],
                                Fecha = Convert.ToDateTime(reader["FechaMonitoreo"]),
                                Recorrido = reader["Recorrido"].ToString(),
                                Jornada = reader["Jornada"].ToString(),
                                Ciclo = reader["Ciclo"].ToString()
                            };

                            if (reader["RegistroId"] != DBNull.Value)
                            {
                                registro = new RegistrosMonitoreo
                                {
                                    Id = Convert.ToInt32(reader["RegistroId"]),
                                    MonitoreoProgramadoId = monitoreo.Id,
                                    Fecha = Convert.ToDateTime(reader["FechaRegistro"]),
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


        [PermisosRol(Rol.Administrador | Rol.Coordinador | Rol.Monitor)]
        [HttpGet]
        public ActionResult Reportes(string supervisor, string docente, string aula, DateTime? fecha)
        {
            List<MonitoreosProgramados> monitoreos = new List<MonitoreosProgramados>();

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                SqlCommand comando = new SqlCommand("sp_ObtenerMonitoreosFiltrados", conexion);
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.AddWithValue("@Supervisor", (object)supervisor ?? DBNull.Value);
                comando.Parameters.AddWithValue("@Docente", (object)docente ?? DBNull.Value);
                comando.Parameters.AddWithValue("@Aula", (object)aula ?? DBNull.Value);
                comando.Parameters.AddWithValue("@Fecha", (object)fecha ?? DBNull.Value);

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
                            Ciclo = reader["Ciclo"].ToString(),
                            TieneMonitoreo = Convert.ToBoolean(reader["TieneMonitoreo"])
                        });
                    }
                }
            }

            ViewBag.SupervisorSeleccionado = supervisor;
            ViewBag.DocenteSeleccionado = docente;
            ViewBag.AulaSeleccionada = aula;
            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

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

        [PermisosRol(Rol.Administrador | Rol.Coordinador | Rol.Monitor)]
        public ActionResult GenerarPDF(string supervisor, string docente, string aula, DateTime? fecha)
        {

            var modelo = new ReportePDFViewModel
            {
                Monitoreos = new List<MonitoreosProgramados>(),
                Registros = new List<RegistrosMonitoreo>()
            };

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                SqlCommand comando = new SqlCommand("sp_ObtenerMonitoreosConRegistros", conexion);
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.AddWithValue("@Docente", string.IsNullOrEmpty(docente) ? (object)DBNull.Value : docente);
                comando.Parameters.AddWithValue("@Aula", string.IsNullOrEmpty(aula) ? (object)DBNull.Value : aula);
                comando.Parameters.AddWithValue("@Fecha", fecha.HasValue ? (object)fecha.Value.Date : DBNull.Value);

                conexion.Open();
                using (SqlDataReader reader = comando.ExecuteReader())
                {
                    var monitoreosDict = new Dictionary<int, MonitoreosProgramados>();

                    while (reader.Read())
                    {
                        int monitoreoId = Convert.ToInt32(reader["MonitoreoId"]);
                        if (!monitoreosDict.ContainsKey(monitoreoId))
                        {
                            var monitoreo = new MonitoreosProgramados
                            {
                                Id = monitoreoId,
                                Materia = reader["Materia"].ToString(),
                                Docente = reader["Docente"].ToString(),
                                Responsable = reader["Responsable"].ToString(),
                                Aula = reader["Aula"].ToString(),
                                Fecha = Convert.ToDateTime(reader["Fecha"])
                            };
                            monitoreosDict[monitoreoId] = monitoreo;
                            modelo.Monitoreos.Add(monitoreo);
                        }

                        if (reader["RegistroId"] != DBNull.Value)
                        {
                            var registro = new RegistrosMonitoreo
                            {
                                Id = Convert.ToInt32(reader["RegistroId"]),
                                MonitoreoProgramadoId = monitoreoId,
                                Fecha = Convert.ToDateTime(reader["FechaRegistro"]),
                                Estado = reader["Estado"].ToString(),
                                Comentarios = reader["Comentarios"] != DBNull.Value ? reader["Comentarios"].ToString() : null,
                                Feedback = reader["Feedback"] != DBNull.Value ? reader["Feedback"].ToString() : null
                            };
                            modelo.Registros.Add(registro);
                        }
                    }
                }
            }

            string nombreUsuario = User.Identity.Name?.Replace(" ", "_") ?? "Usuario";
            string fechaHoy = DateTime.Now.ToString("yyyyMMdd");

            try
            {
                return new ViewAsPdf("_ReportePDF", modelo)
                {
                    FileName = $"Reporte_Monitoreos_{fechaHoy}_{nombreUsuario}.pdf",
                    PageSize = Rotativa.Options.Size.A4,
                    PageMargins = new Rotativa.Options.Margins(10, 10, 10, 10)
                };
            }
            catch (Exception ex)
            {
                return Content("Error al generar PDF: " + ex.Message);
            }
        }

        [PermisosRol(Rol.Administrador | Rol.Coordinador | Rol.Monitor)]
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
        public ActionResult Lineamientos()
        {
            return View();
        }

        //>Endpoint para incrementar y retornar el valor actualizado:
        [HttpPost]
        public JsonResult IncrementarContadorReporte()
        {
            int nuevoTotal = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"UPDATE ReporteContador SET Total = Total + 1, FechaActualizacion = GETDATE() WHERE Id = 1;
                         SELECT Total FROM ReporteContador WHERE Id = 1;";

                SqlCommand cmd = new SqlCommand(query, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    // Saltamos el resultado del UPDATE
                    if (reader.NextResult() && reader.Read())
                    {
                        nuevoTotal = reader.GetInt32(0);
                    }
                }
            }

            return Json(new { total = nuevoTotal });
        }

        //Consulta el valor a recargar la vista
        [HttpGet]
        public JsonResult ObtenerContadorReporte()
        {
            int total = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Total FROM ReporteContador WHERE Id = 1", conn);
                total = (int)cmd.ExecuteScalar();
            }

            return Json(new { total = total }, JsonRequestBehavior.AllowGet);
        }


        //---------------------------cambios 17/05/2025 Cristian Alas---------------------------//

        ////////////////////////////////////////// Generar CSV //////////////////////////////////////////
        [PermisosRol(Rol.Administrador | Rol.Coordinador | Rol.Monitor)]
        [HttpGet]
        public ActionResult GenerarCSV(string supervisor, string docente, string aula, DateTime? fecha)
        {
            var registros = new List<string>();
            registros.Add("Materia,Docente,Aula,Fecha,Responsable,Estado,Comentarios,Feedback");

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                var query = @"SELECT mp.Materia, mp.Docente, mp.Aula, mp.Fecha, mp.Responsable,
                             rm.Estado, rm.Comentarios, rm.Feedback
                      FROM MonitoreosProgramados mp
                      LEFT JOIN RegistrosMonitoreo rm ON mp.Id = rm.MonitoreoProgramadoId
                      WHERE 1=1";
                var parameters = new List<SqlParameter>();

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

                if (!string.IsNullOrEmpty(supervisor))
                {
                    query += " AND mp.Responsable LIKE @Supervisor";
                    parameters.Add(new SqlParameter("@Supervisor", "%" + supervisor + "%"));
                }

                if (fecha.HasValue)
                {
                    query += " AND CONVERT(date, mp.Fecha) = CONVERT(date, @Fecha)";
                    parameters.Add(new SqlParameter("@Fecha", fecha.Value));
                }

                SqlCommand comando = new SqlCommand(query, conexion);
                comando.CommandTimeout = 120;
                foreach (var param in parameters)
                {
                    comando.Parameters.Add(param);
                }

                conexion.Open();
                using (SqlDataReader reader = comando.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var linea = string.Join(",", new string[]
                        {
                    $"\"{reader["Materia"]}\"",
                    $"\"{reader["Docente"]}\"",
                    $"\"{reader["Aula"]}\"",
                    $"\"{Convert.ToDateTime(reader["Fecha"]).ToString("yyyy-MM-dd")}\"",
                    $"\"{reader["Responsable"]}\"",
                    $"\"{reader["Estado"]?.ToString() ?? "N/A"}\"",
                    $"\"{reader["Comentarios"]?.ToString().Replace("\"", "'") ?? "N/A"}\"",
                    $"\"{reader["Feedback"]?.ToString().Replace("\"", "'") ?? "N/A"}\""
                        });

                        registros.Add(linea);
                    }
                }
            }
            string nombreUsuario = User.Identity.Name?.Replace(" ", "_") ?? "Usuario";
            string fechaHoy = DateTime.Now.ToString("yyyyMMdd");

            var contenidoCSV = string.Join(Environment.NewLine, registros);
            var buffer = Encoding.UTF8.GetBytes(contenidoCSV);
            return File(buffer, "text/csv", $"Reporte_Monitoreos_{fechaHoy}_{nombreUsuario}.csv");
        }

        ////////////////////////////////////////// GET Actualizar monitoreo //////////////////////////////////////////
        [HttpGet]
        public ActionResult EditarMonitoreo(int id)
        {
            try
            {
                RegistrosMonitoreo registro = null;

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    string query = @"SELECT Id, MonitoreoProgramadoId, Fecha, Estado, Comentarios 
                             FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";

                    SqlCommand cmd = new SqlCommand(query, conexion);
                    cmd.Parameters.AddWithValue("@Id", id);

                    conexion.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            registro = new RegistrosMonitoreo
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                MonitoreoProgramadoId = Convert.ToInt32(reader["MonitoreoProgramadoId"]),
                                Fecha = Convert.ToDateTime(reader["Fecha"]),
                                Estado = reader["Estado"].ToString(),
                                Comentarios = reader["Comentarios"]?.ToString()
                            };
                        }
                    }
                }

                if (registro == null)
                {
                    return HttpNotFound("Registro no encontrado");
                }

                ViewBag.Estados = new List<string> { "Todo bien", "Nadie en el aula", "Cerrado"};
                return PartialView("_FormularioMonitoreo", registro); // misma vista que para crear
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Content("Error: " + ex.Message);
            }
        }

        ////////////////////////////////////////// POST Actualizar monitoreo //////////////////////////////////////////
        [HttpPost]
        public ActionResult GuardarEdicionMonitoreo(RegistrosMonitoreo registro)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE RegistrosMonitoreo 
                             SET Estado = @Estado, Comentarios = @Comentarios 
                             WHERE Id = @Id";

                    SqlCommand comando = new SqlCommand(query, conexion);
                    comando.Parameters.AddWithValue("@Id", registro.Id);
                    comando.Parameters.AddWithValue("@Estado", registro.Estado);
                    comando.Parameters.AddWithValue("@Comentarios", registro.Comentarios != null ? (object)registro.Comentarios : DBNull.Value);
                    


                    conexion.Open();
                    comando.ExecuteNonQuery();
                }

                return Json(new { success = true });
            }

            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        ////////////////////////////////////////// Eliminar monitoreo //////////////////////////////////////////
        [HttpPost]
        [PermisosRol(Rol.Administrador | Rol.Coordinador)]
        public ActionResult EliminarMonitoreo(int id)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();

                    // Eliminar primero el registro relacionado si existe
                    string deleteRegistro = "DELETE FROM RegistrosMonitoreo WHERE MonitoreoProgramadoId = @Id";
                    SqlCommand cmdReg = new SqlCommand(deleteRegistro, conexion);
                    cmdReg.Parameters.AddWithValue("@Id", id);
                    cmdReg.ExecuteNonQuery();

                    // Luego el monitoreo programado
                    string deleteMonitoreo = "DELETE FROM MonitoreosProgramados WHERE Id = @Id";
                    SqlCommand cmdMon = new SqlCommand(deleteMonitoreo, conexion);
                    cmdMon.Parameters.AddWithValue("@Id", id);
                    int filasAfectadas = cmdMon.ExecuteNonQuery();

                    if (filasAfectadas == 0)
                        return HttpNotFound("No se encontró el monitoreo a eliminar.");
                }

                return new HttpStatusCodeResult(200);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error al eliminar: " + ex.Message);
            }
        }
        [HttpGet]
        public ActionResult CrearEmergencia()
        {
            var usuario = Session["Usuario"] as Usuarios;
            if (usuario == null)
            {
                return RedirectToAction("Index", "Acceso");
            }

            ViewBag.NombreUsuario = usuario.Nombres;

            var modelo = new Emergencia
            {
                NombreMonitor = usuario.Nombres,
                FechaHora = DateTime.Now
            };

            return View(modelo);
        }

        [HttpPost]
        public ActionResult GuardarEmergencia(Emergencia emergencia)
        {
            try
            {
                if (string.IsNullOrEmpty(emergencia.NombreMonitor))
                {
                    TempData["Error"] = "El nombre es requerido.";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(emergencia.Edificio))
                {
                    TempData["Error"] = "El edificio es requerido.";
                    return RedirectToAction("Index", "Home");
                }

                if (emergencia.FechaHora == DateTime.MinValue)
                {
                    TempData["Error"] = "La fecha es requerida.";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(emergencia.Comentarios))
                {
                    emergencia.Comentarios = "Sin comentarios";
                }

                COD_Emergencia logica = new COD_Emergencia();
                logica.GuardarEmergencia(
                    emergencia.NombreMonitor,
                    emergencia.Edificio,
                    emergencia.FechaHora,
                    emergencia.Comentarios
                );

                TempData["Exito"] = "Emergencia enviada con éxito.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hubo un problema al enviar la emergencia: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }


        // Contador de reportes descargados en pdf y csv
        [HttpPost]
        public JsonResult IncrementarContadorPDF()
        {
            int totalPDF = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            UPDATE ReporteContador
            SET TotalPDF = TotalPDF + 1, FechaActualizacion = GETDATE()
            WHERE Id = 1;
            SELECT TotalPDF FROM ReporteContador WHERE Id = 1;
        ";

                SqlCommand cmd = new SqlCommand(query, conn);
                totalPDF = (int)cmd.ExecuteScalar();
            }

            return Json(new { totalPDF });
        }

        [HttpPost]
        public JsonResult IncrementarContadorCSV()
        {
            int totalCSV = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            UPDATE ReporteContador
            SET TotalCSV = TotalCSV + 1, FechaActualizacion = GETDATE()
            WHERE Id = 1;
            SELECT TotalCSV FROM ReporteContador WHERE Id = 1;
        ";

                SqlCommand cmd = new SqlCommand(query, conn);
                totalCSV = (int)cmd.ExecuteScalar();
            }

            return Json(new { totalCSV });
        }

        //cambios 22/05/2023

        [PermisosRol(Rol.Administrador | Rol.Coordinador)]
        [HttpGet]
        public ActionResult ProgramarMonitoreo()
        {
            var monitoreo = new MonitoreosProgramados
            {
                Fecha = DateTime.Today
            };

            return View(monitoreo);
        }

        //[PermisosRol(Rol.Administrador | Rol.Coordinador)]
        //[HttpPost]
        //public ActionResult ProgramarMonitoreo(MonitoreosProgramados monitoreo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        using (SqlConnection conexion = new SqlConnection(connectionString))
        //        {
        //            string query = @"INSERT INTO MonitoreosProgramados
        //(Materia, Docente, Responsable, Aula, HoraInicio, HoraFin, Fecha, Recorrido, Jornada, Ciclo)
        //VALUES (@Materia, @Docente, @Responsable, @Aula, @HoraInicio, @HoraFin, @Fecha, @Recorrido, @Jornada, @Ciclo)";

        //            SqlCommand cmd = new SqlCommand(query, conexion);
        //            cmd.Parameters.AddWithValue("@Materia", monitoreo.Materia);
        //            cmd.Parameters.AddWithValue("@Docente", monitoreo.Docente);
        //            cmd.Parameters.AddWithValue("@Responsable", monitoreo.Responsable);
        //            cmd.Parameters.AddWithValue("@Aula", monitoreo.Aula);
        //            cmd.Parameters.AddWithValue("@HoraInicio", monitoreo.HoraInicio);
        //            cmd.Parameters.AddWithValue("@HoraFin", monitoreo.HoraFin);
        //            cmd.Parameters.AddWithValue("@Fecha", monitoreo.Fecha);
        //            cmd.Parameters.AddWithValue("@Recorrido", monitoreo.Recorrido);
        //            cmd.Parameters.AddWithValue("@Jornada", monitoreo.Jornada);
        //            cmd.Parameters.AddWithValue("@Ciclo", monitoreo.Ciclo);

        //            conexion.Open();
        //            cmd.ExecuteNonQuery();
        //        }

        //        return RedirectToAction("Filtro");
        //    }

        //    return View(monitoreo);
        //}
        ////////////////////////////////////////// Post Programar Monitoreo //////////////////////////////////////////
        [HttpPost]
        [PermisosRol(Rol.Administrador | Rol.Coordinador)]
        public ActionResult ProgramarMonitoreo(MonitoreosProgramados monitoreo)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    monitoreo.EstadoMonitoreo = monitoreo.Fecha.Date < DateTime.Today ? "Expirado" : "Pendiente";

                    string query = @"INSERT INTO MonitoreosProgramados
                    (Materia, Docente, Responsable, Aula, HoraInicio, HoraFin, Fecha, Recorrido, Jornada, Ciclo, EstadoMonitoreo)
                    VALUES (@Materia, @Docente, @Responsable, @Aula, @HoraInicio, @HoraFin, @Fecha, @Recorrido, @Jornada, @Ciclo, @EstadoMonitoreo)";

                    SqlCommand cmd = new SqlCommand(query, conexion);
                    cmd.Parameters.AddWithValue("@Materia", monitoreo.Materia);
                    cmd.Parameters.AddWithValue("@Docente", monitoreo.Docente);
                    cmd.Parameters.AddWithValue("@Responsable", monitoreo.Responsable);
                    cmd.Parameters.AddWithValue("@Aula", monitoreo.Aula);
                    cmd.Parameters.AddWithValue("@HoraInicio", monitoreo.HoraInicio);
                    cmd.Parameters.AddWithValue("@HoraFin", monitoreo.HoraFin);
                    cmd.Parameters.AddWithValue("@Fecha", monitoreo.Fecha);
                    cmd.Parameters.AddWithValue("@Recorrido", monitoreo.Recorrido);
                    cmd.Parameters.AddWithValue("@Jornada", monitoreo.Jornada);
                    cmd.Parameters.AddWithValue("@Ciclo", monitoreo.Ciclo);
                    cmd.Parameters.AddWithValue("@EstadoMonitoreo", monitoreo.EstadoMonitoreo);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("Filtro");
            }

            return View(monitoreo);
        }
    }
}