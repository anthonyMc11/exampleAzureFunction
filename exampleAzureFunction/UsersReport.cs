
//how to use functional settings
//https://blog.jongallant.com/2018/01/azure-function-config/

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using Dapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

namespace AzureFunctionalDemo
{
    public static class UsersReport
    {
        [FunctionName("UsersReport")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            //this is used to get at the settings
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            //how to log messages to debug
            log.Info("C# HTTP trigger function processed a request.");

            //get variable from query string
            string name = req.Query["name"];
          

           
            //get variable from the body
        string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            //this is how you access variables in the host.json
            var str = config.GetConnectionString("sqldb_connection");
           
            var users = new List<UserModel>();

            //line for debugging connection string
            //return new OkObjectResult(str);

            //open a connection to sql server
            using (var conn = new SqlConnection(str))
            {
                conn.Open();
                //select command
                const string text = "SELECT * FROM[dbo].[MyTable]";

                users = conn.Query<UserModel>(text).ToList();

            }

            //write the objects to a csv
            var textWriter = new StringWriter();
            CsvWriter csv = new CsvWriter(textWriter);
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.Delimiter = ",";

            csv.WriteRecords(users);

            //force the file download
            var filename = "Users_report" + DateTime.Now.ToString("yyyy-MMM-dd") + ".csv";

            string downloadText = textWriter.ToString();
      
            var contentType = "text/csv";
           
            var bytes = Encoding.UTF8.GetBytes(downloadText);
            var result = new FileContentResult(bytes, contentType);
            result.FileDownloadName = filename;
            return result;

        }
    }  
}
