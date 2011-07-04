# log4net RabbitMQ Appender

**Nuget key: `log4net.RabbitMQAppender`**

An appender for logging over AMQP, specifically RabbitMQ. Why? Because sometimes you want to log with topics, without deciding on where the data/logs end up. Publish-subscribe, that is. The appender uses topics; a tutorial on topic routing, [can be found at RabbitMQ's web site](http://www.rabbitmq.com/tutorials/tutorial-five-python.html).

Appender properties:

 * **VHost** - the virtual host to use. This needs to be configured in RabbitMQ before put to use. Defaults to `/`.
 * **UserName** - the username to authenticate with. Defaults to `guest`.
 * **Password** - the password to authenticate with. Defaults to `guest`.
 * **Port** - what port the RabbitMQ broker is listening to. Defaults to `5672`.
 * **Topic** - what topic to publish with. It must contain a string: `{0}`, or the logger won't work. The string inserted here will be used together with `string.Format`.
 * **Protocol** - of type IProtocol - what protocol to use for RabbitMQ-communication. See also `SetProtocol`.
 * **HostName** - the host name of the computer/node to connect to. Defaults to `localhost`.
 * **Exchange** - what exchange to publish log messages to. Defaults to `log4net-logging` and is declared when the appender is started.

For SSL -- have a look at: http://www.rabbitmq.com/ssl.html

A guide to setting up a secure corporate messaging infrastructure w/ .Net might be in the works... ;) Keep tuned to @henrikfeldt on twitter!

## Example log4net.config

This configuration demonstrates usage of the properties from above:

```xml
<log4net>
	<appender name="AmqpAppender" type="log4net.RabbitMQ.RabbitMQAppender, log4net.RabbitMQ">
		<topic value="samples.web.{0}" />
		<layout type="log4net.Layout.PatternLayout">
				<conversionPattern value="%date [%thread] %-5level - %message%newline" />
		</layout>
	</appender>
	<root>
		<level value="DEBUG"/>
		<appender-ref ref="AmqpAppender" />
	</root>
</log4net>
```

You would register log4net in a web application as such, in `Application_Start`:

```csharp
using log4net.Config;
// ...
XmlConfigurator.ConfigureAndWatch(new FileInfo(Server.MapPath("~/log4net.config")));
```

In `Application_End`:

```csharp
LogManager.Shutdown();
```

If you put the log4net configuration in web.config, reloading and restarting the AMQP channel won't work after an AppDomain recycle or change to the web.config file.

## From the receiving side

For full documentation, see the RabbitMQ web site. An example receiver has this main method:

```csharp
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
		var q = m.QueueDeclare("", false, true, true, null);

		m.QueueBind(q, "log4net-logging", "#");
		m.BasicConsume(q, true, consumer);
				
		while (true)
			Console.Write(((BasicDeliverEventArgs) consumer.Queue.Dequeue()).Body.AsUtf8String());
	}
}
// ...
static class Extensions {
	public static string AsUtf8String(this byte[] args) {
		return Encoding.UTF8.GetString(args);
	}
}
```

It should be noted that the message's IBasicProperties' following properties are also set:

 * **ContentEncoding** - to "utf8"
 * **ContentType** - to "text/plain"
 * **AppId** - to `loggingEvent.Domain`
 * **Timestamp** - to `new AmqpTimestamp(Convert.ToInt64((loggingEvent.TimeStamp - _Epoch).TotalSeconds))` where _Epoch is 1/1/1970 at 00:00. Hence, it's the unix timestamp of when the log event happened in the application, according to that application's clock.
 
## Final Remarks

Report issues at this repository's **Issues** page.
 
E-mail feedback to henrik at haf dot se or send me a pm over github.

Cheers,
Henrik Feldt