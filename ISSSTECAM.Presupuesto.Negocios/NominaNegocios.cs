using ISSSTECAM.Presupuesto.Datos;
using ISSSTECAM.Presupuesto.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISSSTECAM.Presupuesto.Negocios
{
    public class NominaNegocios
    {
        public static void GuardarNomina(Nomina nueva)
        {
            var transaccion = new Transaccion();
            try
            {
                var repositorio = new Repositorio<Nomina>(transaccion);
                repositorio.Agregar(nueva);
                transaccion.GuardarCambios();
            }
            catch (Exception ex)
            {
                transaccion.Dispose();
            }
        }

        public static IEnumerable<Nomina> ObtenerPorPeriodo(DateTime fechaInicio, DateTime fechaFin)
        {
            var transaccion = new Transaccion();

            var repositorio = new Repositorio<Nomina>(transaccion);
            var nominas =
                repositorio.ObtenerPorFiltro(n => n.FechaAplicacion >= fechaInicio
                    && n.FechaAplicacion <= fechaFin);
            return nominas;

        }
    }
}
