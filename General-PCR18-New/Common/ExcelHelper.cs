using General_PCR18.Util;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Common
{
    public class ExcelHelper
    {

        /// <summary>
        /// 获取单元格类型
        /// </summary>
        /// <param name="cell">目标单元格</param>
        /// <returns></returns>
        private static object GetValueType(ICell cell)
        {
            if (cell == null)
                return null;
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return null;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Numeric:
                    return cell.NumericCellValue;
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Error:
                    return cell.ErrorCellValue;
                case CellType.Formula:
                    // 这里只能处理数值公式。其他公式将会被转换成数值类型，如 日期公式。
                    return cell.NumericCellValue;
                default:
                    return cell.StringCellValue;
            }
        }

        /// <summary>
        /// 设置单元格数据类型
        /// </summary>
        /// <param name="cell">目标单元格</param>
        /// <param name="obj">数据值</param>
        /// <returns></returns>
        public static void SetCellValue(ICell cell, object obj)
        {
            if (obj.GetType() == typeof(int))
            {
                cell.SetCellValue((int)obj);
            }
            else if (obj.GetType() == typeof(double))
            {
                cell.SetCellValue((double)obj);
            }
            else if (obj.GetType() == typeof(IRichTextString))
            {
                cell.SetCellValue((IRichTextString)obj);
            }
            else if (obj.GetType() == typeof(string))
            {
                cell.SetCellValue(obj.ToString());
            }
            else if (obj.GetType() == typeof(DateTime))
            {
                cell.SetCellValue((DateTime)obj);
            }
            else if (obj.GetType() == typeof(bool))
            {
                cell.SetCellValue((bool)obj);
            }
            else
            {
                cell.SetCellValue(obj.ToString());
            }
        }

        /// <summary>
        /// 单元格样式
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="isHead"></param>
        /// <returns></returns>
        private static ICellStyle CreateCellStyle(IWorkbook workbook, bool isHead)
        {
            var cellStyle = workbook.CreateCellStyle();

            var font = workbook.CreateFont();
            font.IsBold = isHead; // 粗体  
            cellStyle.SetFont(font);

            cellStyle.Alignment = HorizontalAlignment.Center; // 水平居中  
            cellStyle.VerticalAlignment = VerticalAlignment.Center; // 垂直居中  

            cellStyle.BorderTop = BorderStyle.Thin;
            cellStyle.BorderBottom = BorderStyle.Thin;
            cellStyle.BorderLeft = BorderStyle.Thin;
            cellStyle.BorderRight = BorderStyle.Thin;
            cellStyle.WrapText = true;//内容自动换行，避免存在换行符的内容合并成单行

            return cellStyle;
        }

        /// <summary>
        /// 将 DataTable 数据保存到Excel文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="tables"></param>
        /// <param name="sheetNames"></param>
        /// <returns></returns>
        public static int DataTableToExecl(string fileName, List<DataTable> tables, List<string> sheetNames)
        {
            IWorkbook workbook = null;
            int total = 0;

            string path = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }


            // 覆盖写入，确保同名文件被替换
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                if (fileName.IndexOf(".xlsx") > 0)
                {
                    workbook = new XSSFWorkbook();
                }
                else if (fileName.IndexOf(".xls") > 0)
                {
                    workbook = new HSSFWorkbook();
                }

                try
                {
                    if (workbook == null)
                    {
                        return 0;
                    }

                    for (int i = 0; i < tables.Count; i++)
                    {
                        var table = tables[i];
                        int count = 0;
                        var sheet = workbook.CreateSheet(sheetNames[i]);

                        // 每列列宽字典
                        var dic = new Dictionary<int, int>();

                        // 标题及内容单元格样式
                        var headCellStyle = CreateCellStyle(workbook, true);
                        var contentCellStyle = CreateCellStyle(workbook, false);

                        // 表头
                        IRow rowHeader = sheet.CreateRow(0);
                        for (int j = 0; j < table.Columns.Count; j++)
                        {
                            ICell cell = rowHeader.CreateCell(j);
                            cell.SetCellValue(table.Columns[j].ToString());
                            cell.CellStyle = headCellStyle;
                            dic.Add(j, Encoding.Default.GetBytes(cell.StringCellValue).Length * 500 + 100);
                        }
                        count++;

                        // 数据
                        for (int j = 0; j < table.Rows.Count; j++)
                        {
                            IRow row = sheet.CreateRow(count);
                            row.HeightInPoints = 20f;

                            for (int k = 0; k < table.Columns.Count; k++)
                            {
                                ICell cell = row.CreateCell(k);
                                cell.CellStyle = contentCellStyle;
                                SetCellValue(cell, table.Rows[j][k]);
                            }
                            count++;
                        }

                        // 设置列宽
                        for (int j = 0; j < table.Columns.Count; j++)
                        {
                            sheet.SetColumnWidth(j, dic[j]);
                        }

                        total += count;
                    }

                    // 写入Excel
                    workbook.Write(fs);
                    workbook.Close();
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                    Console.WriteLine(ex);
                }
            }

            return total;
        }

        /// <summary>
        /// 将 Excel Sheet 存到 DataTable
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public static DataTable SheetToDatatable(ISheet sheet)
        {
            DataTable dt = new DataTable();
            try
            {
                IRow firsRow = sheet.GetRow(0);
                if (firsRow == null)
                {
                    return null;
                }
                int cellCount = firsRow.LastCellNum;

                for (int i = firsRow.FirstCellNum; i < cellCount; i++)
                {
                    ICell cell = firsRow.GetCell(i);
                    // 兼容表头为数值/公式/空单元格的情况
                    string cellValue = cell == null ? string.Empty : cell.ToString();
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        cellValue = $"Column{i}"; // 占位列名，保持列位置不变
                    }
                    DataColumn column = new DataColumn(cellValue);
                    dt.Columns.Add(column);
                }
                int startRow = sheet.FirstRowNum + 1;

                // 最后一行的标号
                int rowCount = sheet.LastRowNum;
                for (int i = startRow; i <= rowCount; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }
                    DataRow dataRow = dt.NewRow();
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        if (row.GetCell(j) != null)
                        {
                            dataRow[j] = GetValueType(row.GetCell(j));
                        }
                    }
                    dt.Rows.Add(dataRow);
                }

                return dt;

            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return null;
            }
        }
    }
}
