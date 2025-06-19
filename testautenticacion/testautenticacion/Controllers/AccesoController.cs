using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using testautenticacion.Models;
using testautenticacion.logica;
using System.Web.Security;

namespace testautenticacion.Controllers
{
    public class AccesoController : Controller
    {
        // GET: Acceso
        public ActionResult Index()
        {
            if (Session["Usuario"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult Index(String correo, string clave)
        {
            Usuarios objeto = new LO_Usuario().EncontrarUsuario(correo, clave);

            if (objeto.Nombres != null)
            {
                //codigo nuevo
                // Verifica si el usuario está activo
                if (!objeto.Estado)
                {
                    ViewBag.Error = "El usuario está deshabilitado.";
                    return View();
                }

                FormsAuthentication.SetAuthCookie(objeto.Correo, false);
                Session["Usuario"] = objeto;
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Correo o contraseña incorrectos";
            return View();
        }
    }
}