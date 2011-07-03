# log4net RabbitMQ Appender

An appender for log4net.

```xml
<appender name="AmqpAppender" type="log4net.RabbitMQ.RabbitMQAppender">
		<layout type="log4net.Layout.PatternLayout">
				<conversionPattern value="%date [%thread] %-5level - %message%newline" />
		</layout>
</appender>
```