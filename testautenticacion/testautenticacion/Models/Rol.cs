using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testautenticacion.Models
{
    [Flags]
    public enum Rol
	{
        //nos permite poner constantes
        Administrador = 1,
        Monitor = 2,
        //
        Coordinador = 4,
    }
}