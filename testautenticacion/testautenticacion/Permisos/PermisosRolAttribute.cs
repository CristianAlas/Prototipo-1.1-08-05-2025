﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using testautenticacion.Models;

namespace testautenticacion.Permisos
{
	public class PermisosRolAttribute : ActionFilterAttribute
	{

		private Rol idrol;

		public PermisosRolAttribute(Rol _idrol)
        {
            this.idrol = _idrol;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.Session["Usuario"] != null)
            {
                Usuarios usuario = HttpContext.Current.Session["Usuario"] as Usuarios;

                if (!this.idrol.HasFlag(usuario.IdRol))
                {
                    filterContext.Result = new RedirectResult("~/Home/SinPermiso");
                }
            }
        }
    }
}