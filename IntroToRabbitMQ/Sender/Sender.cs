using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using Polly;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;

namespace Sender
{
    class Sender
    {
        public static void Main()
        {
            var factory = new ConnectionFactory() {HostName = "localhost"};
            var retryPolicy = Policy.Handle<AlreadyClosedException>()
                .WaitAndRetry(5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"This is the {retryCount} try");
                    }
                );

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "QueueDemo",
                        durable: true,
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
                        Thread.Sleep(1000);
                    }
                }
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static void FillMessages(IList<string> arrayToFill)
        {
            for (var j = 0; j < arrayToFill.Count; j++)
            {
                arrayToFill[j] = $"Message {j}";
            }
        }

        private static void PublishMessage(string message, IModel channel)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "",
                routingKey: "QueueDemo",
                basicProperties: new BasicProperties{DeliveryMode = 2},
                body: body);
            Console.WriteLine($"Sent {message}");
        }
    }
}