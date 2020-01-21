using CrystalDecisions.CrystalReports.Engine;
using ISSSTECAM.Presupuesto.Entidades;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ISSSTECAM.Presupuesto.Web.Controllers
{
    public class NominaController : Controller
    {
        //
        // GET: /Nomina/

        public ActionResult Conceptos()
        {
            return PartialView();
        }

        public ActionResult Cargar()
        {
            return PartialView();
        }
        public ActionResult Tabla()
        {
            return PartialView();
        }

        [HttpPost]
        public JsonResult ImportarArchivo(/*string fechaNomina*/)
        {
            Nomina nueva = new Nomina();

            nueva.Activo = true;
            nueva.FechaAplicacion = DateTime.Now;
        //Convert.ToDateTime(fechaNomina);
            nueva.DetallesNomina = new List<DetallesNomina>();





            List<Concepto> Conceptos = new List<Concepto>();
            //List<CalendarioClavePresupuestal> claves = new List<CalendarioClavePresupuestal>();

            //List<ClavesPresupuestales> claves = new List<ClavesPresupuestales>();

            var archivo = Request.Files[0];
            using (ExcelPackage package = new ExcelPackage(archivo.InputStream))
            {
                //validar que solo tenga una hoja
                if (package.Workbook.Worksheets.Count != 1)
                    return Json(new { exitoso = false, mensaje = "El archivo tiene más de una hoja" });

                var hoja = package.Workbook.Worksheets[1];

                int filas = hoja.Dimension.End.Row;

                //encontrar conceptos
                var ConceptosRegistrados = Negocios.ConceptosNegocios.ObtenerActivos();

                for (int i = 1; i <= hoja.Dimension.End.Column; i++)
                {
                    Debug.Write(hoja.Cells[8, i].Value.ToString());
                    string cabecera = hoja.Cells[8, i].Value.ToString();
                    if (!string.IsNullOrEmpty(cabecera))
                    {
                        var ConceptosEncontrados =
                            ConceptosRegistrados.Where(c => c.ConceptoNomina.Trim().ToUpper() == cabecera.Trim().ToUpper());
                        //if(ConceptosEncontrados.Count() == 1)
                        //{
                        var ConceptoEnLista =
                            Conceptos.FirstOrDefault(c => c.NombreContable == cabecera.Trim().ToUpper() && c.Columna == i);
                        if (ConceptoEnLista == null)
                        {
                            foreach (var concepto in ConceptosEncontrados)
                            {
                                Conceptos.Add(
                                    new Concepto
                                    {
                                        Id = concepto.Id,
                                        Columna = i,
                                        NombreContable = cabecera.Trim().ToUpper(),
                                        NombrePresupuestal = concepto.ConceptoPresupuesto,
                                        TipoEmpleado = concepto.IdTipoEmpleadoAplica
                                    });                                
                            }
                        }
                        //}
                        //else if (ConceptosEncontrados.Count() > 1)
                        //{
                        //    var ConceptoEnLista =
                        //        Conceptos.FirstOrDefault(c => c.NombreContable == cabecera.Trim().ToUpper() 
                        //            && c.Columna == i 
                        //            && c.Id == );
                        //}
                    }
                }

                var servicioRH = new RH.ServiciosWeb.ServicioEmpleadosClient();
                List<string> empleadosInexistentes = new List<string>();
                //encontrar detalles
                for (int fila = 9; fila <= hoja.Dimension.End.Row; fila++)
                {
                    if (hoja.Cells[fila, 1].Value == null)
                        break;
                    var numeroEmpleado = hoja.Cells[fila, 1].Value.ToString();
                    var empleado = servicioRH.ObtenerPorNumeroEmpleado(numeroEmpleado);

                    if (empleado.IdEmpleado == 0)
                    {
                        empleadosInexistentes.Add(numeroEmpleado);
                        continue;
                    }

                    foreach (var concepto in Conceptos)
                    {
                        if (concepto.Detalles == null)
                            concepto.Detalles = new List<DetalleConcepto>();

                        if (concepto.TipoEmpleado.HasValue)
                        {
                            if (concepto.TipoEmpleado == empleado.IdTipoEmpleado)
                            {
                                var detalle = concepto.Detalles.FirstOrDefault(d => d.CentroCosto == empleado.NombreCosto);
                                if (detalle == null)
                                {
                                    concepto.Detalles.Add(
                                    new DetalleConcepto
                                    {
                                        CentroCosto = empleado.NombreCosto,
                                        Total = Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value)
                                    });                                                                                 
                                }
                                else
                                {
                                    detalle.Total += Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value);
                                }

                                nueva.DetallesNomina.Add(
                                    new DetallesNomina
                                    {
                                        IdConcepto = concepto.Id,
                                        IdEmpleado = empleado.IdEmpleado,
                                        IdTipoEmpleado = empleado.IdTipoEmpleado,
                                        Monto = Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value),
                                        CentroCosto = empleado.NombreCosto
                                    });
                            }
                        }
                        else
                        {
                            var detalle = concepto.Detalles.FirstOrDefault(d => d.CentroCosto == empleado.NombreCosto);
                            if (detalle == null)
                            {
                                concepto.Detalles.Add(
                                new DetalleConcepto
                                {
                                    CentroCosto = empleado.NombreCosto,
                                    Total = Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value)
                                });
                            }
                            else
                            {
                                detalle.Total += Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value);
                            }

                            nueva.DetallesNomina.Add(
                                    new DetallesNomina
                                    {
                                        IdConcepto = concepto.Id,
                                        IdEmpleado = empleado.IdEmpleado,
                                        IdTipoEmpleado = empleado.IdTipoEmpleado,
                                        Monto = Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value),
                                        CentroCosto = empleado.NombreCosto
                                    });
                        }

                        //var detalle = 
                        //    concepto.Detalles.FirstOrDefault(d => d.CentroCosto == empleado.NombreCosto && d.TipoEmpleado == empleado.IdTipoEmpleado);
                        //if (detalle == null)
                        //{
                        //    concepto.Detalles.Add(
                        //        new DetalleConcepto
                        //        {
                        //            CentroCosto = empleado.NombreCosto,
                        //            TipoEmpleado = empleado.IdTipoEmpleado,
                        //            Total = Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value)
                        //        });
                        //}
                        //else
                        //{
                        //    detalle.Total += Convert.ToDecimal(hoja.Cells[fila, concepto.Columna].Value);
                        //}

                    }
                }
                Negocios.NominaNegocios.GuardarNomina(nueva);
                string mensajeMontos = "";
                Conceptos.ForEach(c => mensajeMontos += "\n" + c.NombreContable + ": " + c.Detalles.Sum(d => d.Total).ToString("c"));

                //decimal t = 0;
                //Conceptos.ForEach(c => t += c.Detalles.Sum(d => d.Total));

                List<CentroDeCosto> centrosCostos = new List<CentroDeCosto>();

                foreach (var concepto in Conceptos)
                {
                    foreach (var detalle in concepto.Detalles)
                    {
                        var centroEncontrado = centrosCostos.FirstOrDefault(cc => cc.Nombre == detalle.CentroCosto);
                        if (centroEncontrado == null)
                        {
                            centrosCostos.Add(new CentroDeCosto { Nombre = detalle.CentroCosto, Total = 0, Detalles = new List<DetalleCentroDeCosto>() });
                            centroEncontrado = centrosCostos.LastOrDefault();
                        }
                        centroEncontrado.Total += detalle.Total;

                    }
                }

                return Json(new
                {
                    exitoso = true,
                    //total = t,
                    errores = string.Join(",", empleadosInexistentes.ToString()),
                    mensaje = mensajeMontos, //"Se importó correctamente"
                    datos = centrosCostos, //Conceptos
                    conceptos = Conceptos
                    .Where(c => c.Detalles.Sum(d => d.Total) > 0)
                    .GroupBy(c => c.NombreContable)
                    .Select(c => new { Concepto = c.Key.ToString(), Total = c.Sum(x => x.Detalles.Sum(d => d.Total)) })
                });
            }
        }

        public JsonResult TotalesConceptos(/*DateTime fechaInicio, DateTime fechaFin*/)
        {
            DateTime fechaInicio = Convert.ToDateTime("01/01/2019");
            DateTime fechaFin = Convert.ToDateTime("31/12/2019");

            var nominas = Negocios.NominaNegocios.ObtenerPorPeriodo(fechaInicio, fechaFin);
            
            Dictionary<string, decimal> conceptos = new Dictionary<string, decimal>();

            
            foreach (var nomina in nominas)
            {
                var conceptosEncontrados = nomina.DetallesNomina.Where(d => d.Conceptos.IdTipoConcepto == (int)Enumeraciones.TipoConcepto.Percepcion).GroupBy(d => d.Conceptos.ConceptoNomina);
                foreach(var conceptoEncontrado in conceptosEncontrados)
                {
                    if (conceptos.ContainsKey(conceptoEncontrado.Key.ToString()))
                    {
                        conceptos[conceptoEncontrado.Key.ToString()] += conceptoEncontrado.Sum(d => d.Monto);
                    }
                    else
                    {
                        conceptos.Add(conceptoEncontrado.Key.ToString(), conceptoEncontrado.Sum(d => d.Monto));
                    }
                }                                
            }

            /*
            conceptos.Add("SUELDO", 1231253.92M);
            conceptos.Add("SUELDO EVENTUAL", 202441.97M);
            conceptos.Add("DIA ECONOMICO", 2442.86M);
            conceptos.Add("SUBS. X INCAP.", 2433.99M);
            conceptos.Add("P. S. M.", 101513.34M);
            conceptos.Add("SUBS. X INCAP. (EVENTUAL)", 8841.51M);
            conceptos.Add("QUINQUENIO", 4057.45M);
             * */

            return Json(conceptos.Select(c => new { Concepto = c.Key.Trim(), Total = c.Value}), JsonRequestBehavior.AllowGet);
        }

        private Dictionary<string, decimal> ObtenerMontosPorCentroCosto(DateTime fechaInicio, DateTime fechaFin)
        {
            var nominas = Negocios.NominaNegocios.ObtenerPorPeriodo(fechaInicio, fechaFin);

            Dictionary<string, decimal> centrosCostos = new Dictionary<string, decimal>();

            foreach (var nomina in nominas)
            {
                var centrosEncontrados = nomina.DetallesNomina.Where(d => d.Conceptos.IdTipoConcepto == (int)Enumeraciones.TipoConcepto.Percepcion).GroupBy(d => d.CentroCosto);
                foreach (var centroCostoEncontrado in centrosEncontrados)
                {
                    if (centrosCostos.ContainsKey(centroCostoEncontrado.Key.ToString()))
                    {
                        centrosCostos[centroCostoEncontrado.Key.ToString()] += centroCostoEncontrado.Sum(d => d.Monto);
                    }
                    else
                    {
                        centrosCostos.Add(centroCostoEncontrado.Key.ToString(), centroCostoEncontrado.Sum(d => d.Monto));
                    }
                }
            }

            return centrosCostos;
        }

        public JsonResult TotalesCentrosCostos(/*DateTime fechaInicio, DateTime fechaFin*/)
        {
            DateTime fechaInicio = Convert.ToDateTime("01/01/2019");
            DateTime fechaFin = Convert.ToDateTime("31/12/2019");

            var nominas = Negocios.NominaNegocios.ObtenerPorPeriodo(fechaInicio, fechaFin);

            Dictionary<string, decimal> centrosCostos = new Dictionary<string, decimal>();


            foreach (var nomina in nominas)
            {
                var centrosEncontrados = nomina.DetallesNomina.Where(d => d.Conceptos.IdTipoConcepto == (int)Enumeraciones.TipoConcepto.Percepcion).GroupBy(d => d.CentroCosto);
                foreach (var centroCostoEncontrado in centrosEncontrados)
                {
                    if (centrosCostos.ContainsKey(centroCostoEncontrado.Key.ToString()))
                    {
                        centrosCostos[centroCostoEncontrado.Key.ToString()] += centroCostoEncontrado.Sum(d => d.Monto);
                    }
                    else
                    {
                        centrosCostos.Add(centroCostoEncontrado.Key.ToString(), centroCostoEncontrado.Sum(d => d.Monto));
                    }
                }
            }

            /*
            conceptos.Add("SUELDO", 1231253.92M);
            conceptos.Add("SUELDO EVENTUAL", 202441.97M);
            conceptos.Add("DIA ECONOMICO", 2442.86M);
            conceptos.Add("SUBS. X INCAP.", 2433.99M);
            conceptos.Add("P. S. M.", 101513.34M);
            conceptos.Add("SUBS. X INCAP. (EVENTUAL)", 8841.51M);
            conceptos.Add("QUINQUENIO", 4057.45M);
             * */

            return Json(centrosCostos.Select(c => new { Concepto = c.Key.Trim(), Total = c.Value }), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GenerarReporte(/*DateTime fechaInicio, DateTime fechaFin*/)
        {
            DateTime fechaInicio = Convert.ToDateTime("01/01/2019");
            DateTime fechaFin = Convert.ToDateTime("31/12/2019");

            var centrosCostos = ObtenerMontosPorCentroCosto(fechaInicio, fechaFin);

            Datasets.dtsReporteGastosSueldosSalarios dts = new Datasets.dtsReporteGastosSueldosSalarios();

            foreach(var centroCosto in centrosCostos)
            {
                dts.CentroCostoTotal.AddCentroCostoTotalRow(centroCosto.Key, centroCosto.Value, 0, 0, 0, 0);
            }

            ReportDocument rd = new ReportDocument();
            rd.Load(Path.Combine(Server.MapPath("~/Reportes"), "GastosSueldosSalarios.rpt"));

            rd.SetDataSource(dts);

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();


            Stream stream = rd.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/pdf", "GastosSueldosSalarios.pdf"); 
            
        }
    }

    public class DetalleConcepto
    {
        public string CentroCosto { get; set; }
        public decimal Total { get; set; }
    }

    public class Concepto
    {
        public int Id { get; set; }
        public int Columna { get; set; }
        public string NombreContable { get; set; }
        public string NombrePresupuestal { get; set; }
        public Nullable<int> TipoEmpleado { get; set; }
        public List<DetalleConcepto> Detalles;
    }

    public class CentroDeCosto
    {
        public string Nombre { get; set; }
        public decimal Total { get; set; }
        public List<DetalleCentroDeCosto> Detalles;
    }

    public class DetalleCentroDeCosto
    {
        public string Concepto { get; set; }
        public int TipoEmpleado { get; set; }
        public decimal Total { get; set; }
    }
}