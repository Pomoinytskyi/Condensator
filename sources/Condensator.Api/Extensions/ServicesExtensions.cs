using RabbitMQ.Client;

namespace Condensator.Api.Extensions
{
	public static class ServicesExtensions
	{
		public static void InitializeRabitMq(this IServiceCollection services)
		{
			ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
			using (IConnection connection = factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				channel.QueueDeclare(queue: "Download",
									 durable: true,
									 exclusive: false,
									 autoDelete: false,
									 arguments: null);
			}

			services.AddSingleton<ConnectionFactory>(factory);
		}
	}
}
