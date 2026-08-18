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
            var ws = workbook.Worksheets.Add(sheetName);

            // Encabezados
            for (int i = 0; i < columns.Length; i++)
            {
                ws.Cell(1, i + 1).Value = columns[i].Header;
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // Datos
            int fila = 2;
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    var value = row[columns[i].ColumnName];
                    ws.Cell(fila, i + 1).Value = value == DBNull.Value ? "" : value.ToString();
                }
                fila++;
            }

            // Formato tabla
            var totalFilas = Math.Max(dt.Rows.Count + 1, 2);
            var rango = ws.Range(1, 1, totalFilas, columns.Length);
            rango.CreateTable();
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
