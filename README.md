# log4net RabbitMQ Appender

**Nuget key: `log4net.RabbitMQAppender`**

An appender for logging over AMQP, specifically RabbitMQ. Why? Because sometimes you want to log with topics, without deciding on where the data/logs end up. Publish-subscribe, that is. The appender uses topics; a tutorial on topic routing, [can be found at RabbitMQ's web site](http://www.rabbitmq.com/tutorials/tutorial-five-python.html).

Appender properties:

 * **VHost** - the virtual host to use. This needs to be configured in RabbitMQ before put to use.
 * **UserName** - the username to authenticate with.
 * **Password** - the password to authenticate with.
 * **Port** - what port the RabbitMQ broker is listening to.
 * **Topic** - what topic to publish with.
 * **Protocol** - of type IProtocol - what protocol to use for RabbitMQ-communication. See also `SetProtocol`.
 * **HostName** - the host name of the computer/node to connect to.
 * **Exchange** - what exchange to publish log messages to.

*Example log4net.config:*
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

In

## Note

If using it in an ASP.Net application, remember to run

```csharp
LogManager.Shutdown();
```

at `Application_End()`.

See `samples\log4net.RabbitMQ.Web` for a complete example.