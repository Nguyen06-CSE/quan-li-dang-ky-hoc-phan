using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Infrastructure.Export;

public class ExcelExportService : IExcelExportService
{
    public Task<byte[]> XuatExcelAsync<T>(string sheetName, IEnumerable<T> duLieu, List<(string TieuDe, Func<T, object?> LayGiaTri)> cotMap)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);

        // 1. Header row
        for (int colIndex = 0; colIndex < cotMap.Count; colIndex++)
        {
            var cell = worksheet.Cell(1, colIndex + 1);
            cell.Value = cotMap[colIndex].TieuDe;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#8CB450");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        // Freeze row đầu
        worksheet.SheetView.FreezeRows(1);

        // 2. Data rows
        int rowIndex = 2;
        foreach (var item in duLieu)
        {
            for (int colIndex = 0; colIndex < cotMap.Count; colIndex++)
            {
                var cell = worksheet.Cell(rowIndex, colIndex + 1);
                var val = cotMap[colIndex].LayGiaTri(item);

                if (val != null)
                {
                    if (val is decimal decVal)
                    {
                        cell.Value = decVal;
                        cell.Style.NumberFormat.Format = "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (val is int intVal)
                    {
                        cell.Value = intVal;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (val is double dblVal)
                    {
                        cell.Value = dblVal;
                    }
                    else if (val is DateTime dtVal)
                    {
                        cell.Value = dtVal;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                    {
                        cell.Value = val.ToString();
                    }
                }
                else
                {
                    cell.Value = string.Empty;
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E0E6DA");
            }
            rowIndex++;
        }

        // 3. Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
