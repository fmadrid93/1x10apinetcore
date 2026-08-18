using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Domain
{
    public interface IExcelExportService
    {
        FileContentResult ExportarXlsx(
            DataTable dt,
            string sheetName,
            string fileName,
            params (string Header, string ColumnName)[] columns
        );
    }
}
