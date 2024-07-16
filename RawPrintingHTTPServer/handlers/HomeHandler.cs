using Microsoft.Reporting.WinForms;
using Newtonsoft.Json;
using RawPrintingHTTPServer.requests;
using RawPrintingHTTPServer.responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace RawPrintingHTTPServer.handlers
{
    class HomeHandler
    {
        private RawPrintingHTTPServer server;
        private ReportDataSource rs = new ReportDataSource();

        public HomeHandler(RawPrintingHTTPServer server)
        {
            this.server = server;
        }

        private bool WritePrintJobFile(string printjobname, byte[] bindata)
        {
            string replaceSlashWith = "_";
            string sanitized = printjobname.Replace("\\", replaceSlashWith).Replace("/", replaceSlashWith);
            string filePath = ServerConfig.basePath + "\\" + sanitized + ".prn";
            using (FileStream sw = new FileStream(filePath, FileMode.Create))
            {
                sw.Write(bindata, 0, bindata.Length);
            }
            return true;
        }

        public ResponseCode handle(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (req.HttpMethod == "POST")
            {
                return _handlePost(req, resp, accesslog);
            }
            else if (req.HttpMethod == "GET")
            {
                return _handleGet(req, resp, accesslog);
            }
            else
            {
                return ResponseCode.NotFound;
            }
        }

        private ResponseCode _handlePost(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            if (!req.HasEntityBody)
            {
                return ResponseCode.NotFound;
            }

            PrintJobResponse printjobresp = new PrintJobResponse();
            try
            {
                using (Stream body = req.InputStream)
                {
                    Encoding encoding = req.ContentEncoding;
                    using (StreamReader reader = new StreamReader(body, encoding))
                    {
                        string json = reader.ReadToEnd();
                        PrintJobPostBody printjob = ServerConfig.fromJSON<PrintJobPostBody>(json);
                        dynamic bodyData = JsonConvert.DeserializeObject(json);
                        //dynamic kliniks = JsonConvert.DeserializeObject(bodyData.klinik);

                        body.Close();
                        reader.Close();

                        //byte[] bindata = printjob.DataToByteArray();

                        /*bool success = false;
                        if (server.config.testingMode == 1)
                        {
                            success = WritePrintJobFile(printjob.id, bindata);
                        }
                        else if (server.config.testingMode == 0)
                        {
                            success = RawPrintingHelper.SendBytesToPrinter(printjob.printer, bindata, printjob.id);
                        }
                        else
                        {
                            success = RawPrintingHelper.SendBytesToPrinter(printjob.printer, bindata, printjob.id) && WritePrintJobFile(printjob.id, bindata);
                        }*/
                        LocalReport localReport = new LocalReport();
                        localReport.ReportPath = Application.StartupPath + "\\Report1.rdlc";
                        ReportParameter[] rptParams = new ReportParameter[]
                        {
                            new ReportParameter("tanggal_jam", bodyData.klinik.nm_klinik.ToString()),
                            new ReportParameter("nm_klinik", bodyData.klinik.nm_klinik.ToString()),
                            new ReportParameter("alm_klinik", bodyData.klinik.alm_klinik.ToString()),
                            new ReportParameter("sip", bodyData.klinik.sip.ToString()),
                            new ReportParameter("nama", bodyData.pasien.nama.ToString()),
                            new ReportParameter("umur",bodyData.pasien.umur.ToString()),
                            new ReportParameter("jk",bodyData.pasien.jk.ToString()),
                            new ReportParameter("alm_pasien",bodyData.pasien.alamat.ToString()),
                            new ReportParameter("catatan",bodyData.catatan.ToString()),
                            new ReportParameter("dokter_nm",bodyData.dokter_nm.ToString()),
                        };
                        localReport.SetParameters(rptParams);
                        List<DataObats> obats = new List<DataObats>();
                        obats.Clear();
                        int i = 1;
                        foreach (var item in bodyData.obats)
                        {
                            byte[] dosisby = Encoding.Default.GetBytes(item.dosis.ToString());
                            string output = Encoding.UTF8.GetString(dosisby);
                            obats.Add(new DataObats
                            {
                                no = i++,
                                nama = item.nama,
                                jumlah = item.jml,
                                dosis = output,
                                keterangan = item.keterangan
                            });
                        }
                        rs.Name = "DataSetRs";
                        rs.Value = obats;
                        localReport.DataSources.Add(rs);
                        string PrinterName = bodyData.printer;

                        localReport.PrintToPrinter(PrinterName, 0, 0);
                        //localReport.ReportPath = "C:\\Users\\lenovo-ecang\\source\\repos\\ecangsandy\\RawPrintingHTTPServer\\RawPrintingHTTPServer\\Report1.rdlc";
                        //accesslog += "\tsuccess\t" + printjob.id + "\t" + printjob.printer;
                        //ServerConfig.appendLog(accesslog);
                        //printjobresp.success = true;
                        printjobresp.data = printjob.id;
                    }
                }
            }
            catch (Exception e)
            {
                ServerConfig.appendLog("Error: " + e.Message + "\n" + e.StackTrace);
                printjobresp.success = false;
                printjobresp.data = ("Error: " + e.Message + "\n" + e.StackTrace);
                accesslog += "\tfailed";
                ServerConfig.appendLog(accesslog);
            }
            server.responseJSON(resp, printjobresp);

            return ResponseCode.OK;
        }

        private ResponseCode _handleGet(HttpListenerRequest req, HttpListenerResponse resp, string accesslog)
        {
            string html = "<html>";
            html += "<head><style>";
            html += ".indent {margin-left: 1.5em;}";
            html += "</style>";
            html += "<script>";
            html += "function ready() {\n";
            html += "var radios = document.querySelectorAll('input[type=radio][name=testmode]');\n";
            html += "function changeHandler(event) {\n";
            html += "var newValue = Number(event.target.value);";
            html += "var xhttp = new XMLHttpRequest();";
            html += "xhttp.onreadystatechange = function() {";
            html += "if (this.readyState == 4 && this.status == 200) {";
            html += "console.log('settings changed');";
            html += "}};\n";
            html += "xhttp.open(\"POST\", \"/settings\", true);";
            html += "xhttp.setRequestHeader( 'Content-Type', 'application/json' );\n";
            html += "var formData = {}; formData.testingMode = newValue;";
            html += "xhttp.send(JSON.stringify(formData));\n";
            html += "}\n";
            html += "Array.prototype.forEach.call(radios, function(radio) {";
            html += "radio.addEventListener('change', changeHandler);";
            html += "});}";
            html += "function removeDomain(domain, idx) {";
            html += "var xhttp = new XMLHttpRequest();";
            html += "xhttp.onreadystatechange = function() {";
            html += "if (this.readyState == 4 && this.status == 200) {";
            html += "var domainEl = document.getElementById(\"domain\" + idx);";
            html += "document.getElementById(\"domainlist\").removeChild(domainEl);";
            html += "}};";
            html += "xhttp.open(\"POST\", \"/permissions\", true);";
            html += "xhttp.setRequestHeader( 'Content-Type', 'application/x-www-form-urlencoded' );";
            html += "var formData = 'host=' + encodeURIComponent(domain) + '&status=remove';";
            html += "xhttp.send(formData);";
            html += "}</script>";
            html += "</head>";
            html += "<body>";
            html += "<h1>" + System.Environment.MachineName + "\\" + Environment.UserName + "</h1>";
            html += "<h2>Listen Port</h2>";
            html += "<p class=indent>" + server.config.port + "</p>";
            html += "<h2>Testing Mode</h2>";
            html += "<p class=indent>";
            html += "<input type=radio id=printer name=testmode value=0";
            if (server.config.testingMode == 0)
            {
                html += " checked";
            }
            html += "><label for=printer> Printer Only </label>";
            html += "<input type=radio id=file name=testmode value=1";
            if (server.config.testingMode == 1)
            {
                html += " checked";
            }
            html += "><label for=file> File Only</label>";
            html += "<input type=radio id=both name=testmode value=2";
            if (server.config.testingMode == 2)
            {
                html += " checked";
            }
            html += "><label for=both> Both</label>";
            html += "</p>";
            html += "<h2>Printers</h2>";
            html += "<div class=indent>";
            html += "<ul>";
            foreach (string printer in ServerConfig.listPrinters())
            {
                html += "<li>" + printer + "</li>";
            }
            html += "</ul>";
            html += "</div>";
            html += "<h2>Allowed Domains</h2>";
            html += "<div class=indent>";
            html += "<ul id=domainlist>";
            int ctr = 0;
            foreach (string domain in server.config.allowedDomains)
            {
                html += "<li id=domain" + ctr + ">" + domain + "&nbsp;<button onclick=\"removeDomain('" + domain + "', " + ctr + ");\">Remove</button></li>";
                ctr++;
            }
            html += "</ul>";
            html += "</div>";
            html += "<script>";
            html += "(function() { ready(); })();";
            html += "</script>";
            html += "</body>";
            html += "</html>";
            ServerConfig.appendLog(accesslog);
            server.responseHTML(resp, html);
            return ResponseCode.OK;
        }
    }
}
