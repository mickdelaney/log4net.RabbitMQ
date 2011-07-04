# log4net RabbitMQ Appender

**Nuget key: `log4net.RabbitMQAppender`**

An appender for log4net. Configure it like this:

```xml
<appender name="AmqpAppender" type="log4net.RabbitMQ.RabbitMQAppender">
		<layout type="log4net.Layout.PatternLayout">
				<conversionPattern value="%date [%thread] %-5level - %message%newline" />
		</layout>
</appender>
```

Example config:

```xml
<log4net>
	<appender name="AmqpAppender" type="log4net.RabbitMQ.RabbitMQAppender">
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

## Note

If using it in an ASP.Net application, remember to run

```csharp
LogManager.Shutdown();
```

at `Application_End()`.