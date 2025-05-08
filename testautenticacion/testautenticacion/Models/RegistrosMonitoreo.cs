using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testautenticacion.Models
{
	public class RegistrosMonitoreo
	{
        public int Id { get; set; }
        public int MonitoreoProgramadoId { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public string Comentarios { get; set; }
        public string Feedback { get; set; }

        // Navigation property
        public virtual MonitoreosProgramados MonitoreoProgramado { get; set; }
    }
}