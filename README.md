# log4net RabbitMQ Appender

**Nuget key: `log4net.RabbitMQAppender`**

An appender for logging over AMQP, specifically RabbitMQ. Why? Because sometimes you want to log with topics, without deciding on where the data/logs end up. Publish-subscribe, that is. The appender uses topics; a tutorial on topic routing, [can be found at RabbitMQ's web site](http://www.rabbitmq.com/tutorials/tutorial-five-python.html).

Appender properties:

 * **VHost** - the virtual host to use. This needs to be configured in RabbitMQ before put to use.
 * **UserName** - the username to authenticate with.
 * **Password** - the password to authenticate with.
 * **Port** - what port the RabbitMQ broker is listening to.
 * **Topic** - what topic to publish with. It must contain a string: `{0}`, or the logger won't work. The string inserted here will be used together with `string.Format`.
 * **Protocol** - of type IProtocol - what protocol to use for RabbitMQ-communication. See also `SetProtocol`.
 * **HostName** - the host name of the computer/node to connect to.
 * **Exchange** - what exchange to publish log messages to.

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