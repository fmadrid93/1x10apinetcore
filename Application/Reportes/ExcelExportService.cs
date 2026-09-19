using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace Application.Reportes
{
    public class ExcelExportService : IExcelExportService
    {
        public FileContentResult ExportarXlsx(
            DataTable dt,
            string sheetName,
            string fileName,
            params (string Header, string ColumnName)[] columns)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Datos" : sheetName);

            // Estilo Encabezados
            for (int i = 0; i < columns.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Datos
            int fila = 2;
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    var colName = columns[i].ColumnName;
                    var value = (dt.Columns.Contains(colName) && row[colName] != DBNull.Value) 
                        ? row[colName].ToString() 
                        : "";
                    
                    var cell = ws.Cell(fila, i + 1);
                    cell.Value = value;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    if (fila % 2 == 1)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    }
                }
                fila++;
            }

            // Ajustar anchos y bordes limpios sin CreateTable para máxima compatibilidad móvil/web
            var totalFilas = Math.Max(dt.Rows.Count + 1, 1);
            var rangoTotal = ws.Range(1, 1, totalFilas, Math.Max(columns.Length, 1));
            rangoTotal.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoTotal.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            rangoTotal.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rangoTotal.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new FileContentResult(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }
    }
}
