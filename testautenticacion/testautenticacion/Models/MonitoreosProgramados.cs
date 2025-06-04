using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace testautenticacion.Models
{
	public class MonitoreosProgramados
	{
        public int Id { get; set; }
        public string Materia { get; set; }
        public string Docente { get; set; }
        public string Responsable { get; set; }
        public string Aula { get; set; }
        public TimeSpan HoraInicio { get; set; }  // Nuevo campo
        public TimeSpan HoraFin { get; set; }     // Nuevo campo
        public DateTime Fecha { get; set; }
        public string Recorrido { get; set; }
        public string Jornada { get; set; }
        public string Ciclo { get; set; }

        public string EstadoMonitoreo { get; set; }
        public bool TieneMonitoreo { get; set; }
        //llenado de tabla para seleccionar responsable de monitoreo desde tabla usuarios
        public IEnumerable<SelectListItem> Monitores { get; set; }


    }
}