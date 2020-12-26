using System;
using System.IO;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MailKit.Net.Smtp;
using MimeKit;

namespace webTrackerContinuous
{
    public class Function1
    {
        [FunctionName("Function1")]
        public static async System.Threading.Tasks.Task RunAsync([TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            await GetAllAvailabileWebsitesAsync();

            log.LogInformation($"Function stopped at: {DateTime.Now}");

        }

        public static string ScrapeWebsite(string url, string email)
        {
            //Replace with the url of this https://github.com/jawadjawid/trackerAutomation function after running it locally(the port could be diff)
            string uri = $"http://localhost:7071/api/screenshot?url={url}&email={email}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";

            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();
            var responseReader = new StreamReader(webStream);
            var response = responseReader.ReadToEnd();
            Console.WriteLine("Response: " + response);
            responseReader.Close();
            return response;
        }

        public static async System.Threading.Tasks.Task GetAllAvailabileWebsitesAsync()
        {
            var client = new MongoClient("mongodb://jawad:jawad@cluster0-shard-00-00.r6ob1.azure.mongodb.net:27017,cluster0-shard-00-01.r6ob1.azure.mongodb.net:27017,cluster0-shard-00-02.r6ob1.azure.mongodb.net:27017/test?ssl=true&replicaSet=atlas-85mm4q-shard-0&authSource=admin&retryWrites=true&w=majority");
            var database = client.GetDatabase("mydb");
            var collection = database.GetCollection<BsonDocument>("customers");

            var documents = await collection.Find(new BsonDocument()).ToListAsync();

            for (int i = 0; i < documents.Count; i++)
            {
                ObjectId id = documents[i].GetValue("_id").AsObjectId;
                string url = documents[i].GetValue("url").AsString;
                string email = documents[i].GetValue("email").AsString;
                string text = documents[i].GetValue("text").AsString;
                if(!ScrapeWebsite(url, email).Equals(text))
                {
                    SendEmail(url, email);
                    var deleteFilter = Builders<BsonDocument>.Filter.Eq("_id", id);
                    collection.DeleteOne(deleteFilter);
                }
            }
        }

        public static void SendEmail(string url, string email)
        {
            MimeMessage message = new MimeMessage();

            MailboxAddress from = new MailboxAddress("Admin",
            "youremail@gmail.com");
            message.From.Add(from);

            MailboxAddress to = new MailboxAddress("User",
            email);
            message.To.Add(to);

            message.Subject = $"{url} has changed";

            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = "<h1>The website you were tracking has changed</h1>";
            bodyBuilder.TextBody = "The website you were tracking has changed!";

            message.Body = bodyBuilder.ToMessageBody();

            SmtpClient client = new SmtpClient();
            client.Connect("smtp.gmail.com", 465, true);
            //Replace with your Gmail and password
            client.Authenticate("youremail@gmail.com", "password");


            client.Send(message);
            client.Disconnect(true);
            client.Dispose();
        }
    }
}
