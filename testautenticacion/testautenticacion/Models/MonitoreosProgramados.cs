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
        public List<SelectListItem> Edificio { get; set; } //usado para cargar la lista de edificios
        public List<SelectListItem> Materias { get; set; } //usado para cargar la lisa de materias
        public string Aula { get; set; }
        public TimeSpan HoraInicio { get; set; }  // 
        public TimeSpan HoraFin { get; set; }     //
        public DateTime Fecha { get; set; }
        public string Recorrido { get; set; }
        public string Jornada { get; set; }
        public string Ciclo { get; set; }

        public string EstadoMonitoreo { get; set; }
        public bool TieneMonitoreo { get; set; }
        //llenado de tabla para seleccionar responsable de monitoreo desde tabla usuarios
        public IEnumerable<SelectListItem> Monitores { get; set; }
        public int IdAula { get; set; }
        public SelectList Aulas { get; set; }  // para mostrar en el formulario



    }
}