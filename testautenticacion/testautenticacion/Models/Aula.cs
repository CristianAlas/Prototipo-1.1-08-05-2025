using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testautenticacion.Models
{
    public class Aula
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int IdEdificio { get; set; }
    }
}