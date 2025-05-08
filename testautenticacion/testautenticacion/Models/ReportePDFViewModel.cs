using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testautenticacion.Models
{
	public class ReportePDFViewModel
	{
        public List<MonitoreosProgramados> Monitoreos { get; set; }
        public List<RegistrosMonitoreo> Registros { get; set; }
    }
}