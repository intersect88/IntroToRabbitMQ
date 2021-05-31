using System;
using System.Linq;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using Polly;
using RabbitMQ.Client.Exceptions;

namespace Sender
{
    class Sender
    {
        public static void Main()
        {
            var factory = new ConnectionFactory() {HostName = "localhost"};
            // Unhandled Exception: RabbitMQ.Client.Exceptions.BrokerUnreachableException: None of the specified endpoints were reachable
            var retryPolicy = Policy.Handle<AlreadyClosedException>()
                .WaitAndRetry(5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"This is the {retryCount} try");
                    }
                    // .Retry(3, onRetry: (exception, i) => 
                );

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "QueueDemo",
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                    var messages = new string [100];

                    FillMessages(messages);

                    foreach (var message in messages)
                    {
                        retryPolicy.Execute(() =>
                            PublishMessage(message, channel)
                        );
                        Thread.Sleep(2000);
                    }
                }
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static void FillMessages(string[] i)
        {
            for (var j = 0; j < i.Length; j++)
            {
                i[j] = $"Message {j}";
            }
        }

        private static void PublishMessage(string message, IModel channel)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "",
                routingKey: "QueueDemo",
                basicProperties: null,
                body: body);
            Console.WriteLine($"Sent {message}");
        }
    }
}