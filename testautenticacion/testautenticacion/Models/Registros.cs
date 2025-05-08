using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testautenticacion.Models
{
	public class Registros
	{
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; }
        public string Docente { get; set; }
        public TimeSpan Hora { get; set; }
        public string Aula { get; set; }
        public string Estado { get; set; }
        public string Comentarios { get; set; }
        public string Feedback { get; set; }
    }
}