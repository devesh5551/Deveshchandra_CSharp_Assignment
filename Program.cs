using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScottPlot;
public class Program {
    public static async Task  Main(string[] args) {
        var _client = new HttpClient();
        try
        {
            string Url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
            var json = await _client.GetStringAsync(Url);
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);
            
           // int i = 1;
            dt.Columns.Add("WorkingHours", typeof(double));
            int namemaxlength = 0; ;

            foreach (DataRow dr in dt.Rows)
            {
                int namelength = dr["EmployeeName"].ToString().Length;
                if (namelength > namemaxlength) namemaxlength = namelength;
                DateTime starTime = Convert.ToDateTime(dr["StarTimeUtc"]);
                DateTime endTime = Convert.ToDateTime(dr["EndTimeUtc"]);
                dr["WorkingHours"] = ((endTime - starTime).TotalMinutes) / 60;

            }
            List<Employee> employees = dt.AsEnumerable()
            .GroupBy(e => e["EmployeeName"].ToString())
            .Select(g => new Employee
            {
                EmployeeName = g.Key,
                TotalWorkingHours = Math.Round(
                    g.Sum(row => row.Field<double>("WorkingHours")), 2)
            })
            .OrderByDescending(e => e.TotalWorkingHours).ToList();

            // Console.Write("SrNo  Employee Name         Working Hours");

            PrintTable(employees, namemaxlength);
            GenerateHTMLFile(employees);
            GeneratePNG(employees);




        }
        catch (Exception ex)
        {
           // Console.WriteLine("Something went wrong while fetching data");
            Console.WriteLine($"Something went wrong while fetching data: {ex.Message}");

        }

    }

    public static void print(string ch, int length) {
        for (int i = 0; i < length; i++) {
            Console.Write(ch);
        }
    }

    #region PrintTable
    //This Method is used to print Table.
    //where namemaxlength variable gets highest name character length because if someone name is Raj Roy and anothername like Deveshchandra Shrivastav.
    //then may be name and working hours column can be colapsed then we find out max length and if check current length after that empty space will be print 
    //empty space or '-' will be print by print()

    static void PrintTable(List<Employee> employees, int namemaxlength) {
        Console.Write("SrNo");
        print(" ", 2);
        Console.Write("Employee Name");
        print(" ", namemaxlength - 13 + 2);
        Console.WriteLine("Working Hours");
        print("-", 4);
        print(" ", 2);
        print("-", namemaxlength);
        print(" ", 2);
        print("-", 13);
        Console.WriteLine();
        int i = 1;
        foreach (Employee e in employees)
        {
            Console.Write(i);
            print(" ", 6 - (Convert.ToString(i).Length));
            string empName = Convert.ToString(e.EmployeeName);
            Console.Write(empName);
            print(" ", namemaxlength - empName.Length + 2);
            Console.WriteLine(e.TotalWorkingHours);
            i++;

        }
    }
    #endregion

    #region GenerateHTMLFile

    // This method is create for Design HTML Page
    // This Method is Save HTML File and Open in browser and print FilePath to console.
    //
    static void GenerateHTMLFile(List<Employee> employees) {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<title>Employee Report</title>");

        sb.AppendLine("<style>");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; background-color: white; font-family: Arial, sans-serif; }");
        sb.AppendLine("table th { background-color: #243b55; color: white; padding: 14px; text-align: left; border: 1px solid #ddd; }");
        sb.AppendLine("table td { padding: 12px; border: 1px solid #ddd; color: #333; }");
        sb.AppendLine("table tr:hover { background-color: #f2f6fc; }");
        sb.AppendLine(".red-row { background-color: #ffcccc !important; }");
        sb.AppendLine(".white-row { background-color: #ffffff !important; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<table > " +
            "<thead>        " +
            "<tr>" +
            "<th>SrNo</th>" +
            "<th>EmployeeName</th>            " +
            "<th>Total Working Hours</th>       " +
            " </tr>    " +
            "</thead>   " +
            " <tbody>       ");
        int i = 1;
        foreach (Employee e in employees)
        {
            string bg = e.TotalWorkingHours < 100 ? "#ffcccc" : "#ffffff";

            sb.AppendLine($"<tr style='border-bottom:1px solid #edf0f5; background-color:{bg};'>" +
          $"<td>{i}</td>" +
          $"<td>{WebUtility.HtmlEncode(e.EmployeeName ?? "Unknown")}</td>" +
          $"<td>{e.TotalWorkingHours:0.00}</td>" +
          "</tr>");
            i++;

        }
        sb.AppendLine("</table>");
        sb.AppendLine("</body></html>");
        string folder = Path.Combine(AppContext.BaseDirectory, "Output");
        Directory.CreateDirectory(folder);
        string htmlPath = Path.Combine(folder, "EmployeeData.html");
        File.WriteAllText(htmlPath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"Generated HTML File Path: {htmlPath}");

        // 3. Optional: open it in the default browser
        Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
    }

    #endregion

    #region GeneratePNG
    //This Method is Used to Generate PNG File Basic of emp data.
    //This Will Generate File and open file in your desktop
    public static void GeneratePNG(List<Employee> emp)
    {


        //List<Employee> emp = Session["EmployeeData"] as List<Employee>;

        double total = emp.Sum(e => e.TotalWorkingHours);
        var palette = new ScottPlot.Palettes.Category10();

        var slices = new List<PieSlice>();
        int i = 0;
        foreach (var item in emp)
        {
            double percent = item.TotalWorkingHours / total * 100;
            string name = string.IsNullOrWhiteSpace(item.EmployeeName) ? "Unknown" : item.EmployeeName;
            slices.Add(new PieSlice
            {
                Value = item.TotalWorkingHours,
                FillColor = GetSliceColor(i++),
                Label = $"{name} ({percent:0.0}%)"
            });
        }

        var plot = new Plot();
        plot.Add.Pie(slices);
        plot.Axes.Frameless();
        plot.HideGrid();
        plot.ShowLegend();
        plot.Title("Working Hours of Employee");

        //plot.SavePng("piechart.png", 700, 500);string folder = Server.MapPath("~/Content/Charts")
         string folder = Path.Combine(AppContext.BaseDirectory, "Output");
            Directory.CreateDirectory(folder);
            string htmlPath = Path.Combine(folder, "report.html");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string filename = "piechart_" + DateTime.Now.ToString("dd_MM_yyyy_mm_hh") + ".png";
        string path = Path.Combine(folder, filename);
        plot.SavePng(path, 700, 500);
        path = Path.Combine(folder, filename);

        if (!System.IO.File.Exists(path))
            Console.WriteLine("File Not Found");
        Console.WriteLine($"Generated HTML File Path: {path}");

        // 3. Optional: open it in the default browser
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        // return File(path, "image/png", filename);


    }
    #endregion

    static ScottPlot.Color GetSliceColor(int index)
    {
        // 0.618 (golden ratio) scatters hues so neighboring slices look different
        float hue = (index * 0.618034f) % 1f;
        return ScottPlot.Color.FromHSL(hue, 0.6f, 0.55f);
    }

}
public class Employee
{
    public string EmployeeName { get; set; }
    public double TotalWorkingHours { get; set; }
}




