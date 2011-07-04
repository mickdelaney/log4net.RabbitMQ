using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace log4net.RabbitMQ.Listener
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest",
				Protocol = Protocols.DefaultProtocol
			};

			using (var c = factory.CreateConnection())
			using (var m = c.CreateModel())
			{
				var consumer = new QueueingBasicConsumer(m);
				var props = new Dictionary<string, object>()
				{
				    {"x-expires", 30*60000} // expire queue after 30 minutes, see http://www.rabbitmq.com/extensions.html
				};
				var q = m.QueueDeclare("", false, true, false, props);

				m.QueueBind(q, "log4net-logging", "#");
				m.BasicConsume(q, true, consumer);
				
				while (true)
					Console.Write(((BasicDeliverEventArgs) consumer.Queue.Dequeue()).Body.AsUtf8String());
			}
		}
	}

	static class Extensions {
		public static string AsUtf8String(this byte[] args) {
			return Encoding.UTF8.GetString(args);
		}
	}
}