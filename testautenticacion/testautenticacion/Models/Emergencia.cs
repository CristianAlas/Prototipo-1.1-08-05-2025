using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testautenticacion.Models
{
    public class Emergencia
    {
        public int Id { get; set; }
        public string NombreMonitor { get; set; }
        public string Edificio { get; set; }
        public DateTime FechaHora { get; set; }
        public string Comentarios { get; set; }
    }
}