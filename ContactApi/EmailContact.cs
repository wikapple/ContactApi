using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using Azure;
using Azure.Communication.Email;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ContactApi
{
    public class EmailContact
    {
        private readonly ILogger<EmailContact> _logger;
        public EmailContact(ILogger<EmailContact> logger, IConfiguration config)
        {
            _logger = logger;
        }

        [Function("EmailContact")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("Contact form submission received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string name = data?.name;
            string email = data?.email;
            string message = data?.message;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(message))
            {
                return new BadRequestObjectResult("Name, email, and message are required.");
            }

            string connectionString = Environment.GetEnvironmentVariable("WIKAPPLE_COMMUNICATION_SERVICE_CONNECTION_STRING");

            var emailClient = new EmailClient(connectionString);

            var emailMessage = new EmailMessage(
                senderAddress: "DoNotReply@fd8e0810-158b-4d1d-9b51-ca09e352684b.azurecomm.net",
                content: new EmailContent($"Profile Website: {name} requested to get in touch")
                {
                    PlainText = $"Somone requested to get in touch on the profile website.\nName:{name}\nemail:{email}\nmessage:{message}",
                    Html = @$"
		                    <html>
			                    <body>
				                    <h1>Somone requested to get in touch on the profile website.</h1>
                                    <ul>
                                        <li>name: {name}</li>
                                        <li>email: {email}</li>
                                        <li>message: <p>{message}</p></li>
                                    </ul>
			                    </body>
		                    </html>"
                },
                recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress("wikapple@gmail.com") })
            );


            EmailSendOperation emailSendOperation = emailClient.Send(WaitUntil.Completed, emailMessage);

            if (emailSendOperation.HasCompleted)
            {
                return new StatusCodeResult(204);
            }

            return new StatusCodeResult(500);


        }
    }
}
