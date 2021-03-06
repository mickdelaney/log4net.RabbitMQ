﻿using System;
using System.Configuration;
using System.IO;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using log4net.Appender;
using log4net.Core;

namespace log4net.RabbitMQ
{
	/// <summary>
	/// An appender for log4net that publishes to RabbitMQ.
	/// </summary>
	public class RabbitMQAppender : AppenderSkeleton
	{
		private string _Exchange = "log4net-logging";
		private IConnection _Connection;
		private IModel _Model;
		private readonly Encoding _Encoding = Encoding.UTF8;
		private readonly DateTime _Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		#region Properties

		private string _VHost = "/";

		/// <summary>
		/// Gets or sets the virtual host to publish to.
		/// </summary>
		public string VHost
		{
			get { return _VHost; }
			set { if (value != null) _VHost = value; }
		}

		private string _UserName = "guest";

		/// <summary>
		/// Gets or sets the username to use for
		/// authentication with the message broker. The default
		/// is 'guest'
		/// </summary>
		public string UserName
		{
			get { return _UserName; }
			set { _UserName = value; }
		}

		private string _Password = "guest";

		/// <summary>
		/// Gets or sets the password to use for
		/// authentication with the message broker.
		/// The default is 'guest'
		/// </summary>
		public string Password
		{
			get { return _Password; }
			set { _Password = value; }
		}

		private uint _Port = 5672;

		/// <summary>
		/// Gets or sets the port to use
		/// for connections to the message broker (this is the broker's
		/// listening port).
		/// The default is '5672'.
		/// </summary>
		public uint Port
		{
			get { return _Port; }
			set { _Port = value; }
		}

		private string _Topic = "{0}";

		/// <summary>
		/// 	Gets or sets the routing key (aka. topic) with which
		/// 	to send messages. Defaults to {0}, which in the end is 'error' for log.Error("..."), and
		/// 	so on. An example could be setting this property to 'ApplicationType.MyApp.Web.{0}'.
		///		The default is '{0}'.
		/// </summary>
		public string Topic
		{
			get { return _Topic; }
			set { _Topic = value; }
		}

		private IProtocol _Protocol = Protocols.DefaultProtocol;

		/// <summary>
		/// Gets or sets the AMQP protocol (version) to use
		/// for communications with the RabbitMQ broker. The default 
		/// is the RabbitMQ.Client-library's default protocol.
		/// </summary>
		public IProtocol Protocol
		{
			get { return _Protocol; }
			set { if (value != null) _Protocol = value; }
		}

		/// <summary>
		/// 	Sets the protocol from a string.
		/// 	Uses <see cref = "Protocols.Lookup" /> internally. It is
		///		safe to pass both null and invalid protocols to this method (but
		///		don't expect those invalid protocols to be used ;))
		/// </summary>
		/// <param name = "protocol"></param>
		public void SetProtocol(string protocol)
		{
			try
			{
				var safeLookup = Protocols.SafeLookup(protocol);

				if (safeLookup != null)
					Protocol = safeLookup;
			}
			catch (ConfigurationException e)
			{
				ErrorHandler.Error(string.Format("could not find protocol named '{0}'", protocol), e);
			}
		}

		private string _HostName = "localhost";

		/// <summary>
		/// 	Gets or sets the host name of the broker to log to.
		/// </summary>
		/// <remarks>
		/// 	Default is 'localhost'
		/// </remarks>
		public string HostName
		{
			get { return _HostName; }
			set { if (value != null) _HostName = value; }
		}

		/// <summary>
		/// 	Gets or sets the exchange to bind the logger output to.
		/// </summary>
		/// <remarks>
		/// 	Default is 'log4net-logging'
		/// </remarks>
		public string Exchange
		{
			get { return _Exchange; }
			set { if (value != null) _Exchange = value; }
		}

		private string _AppId;
		/// <summary>
		/// Gets or sets the application id to specify when sending. Defaults to null,
		/// and then IBasicProperties.AppId will be the name of the logger instead.
		/// </summary>
		public string AppId
		{
			get { return _AppId; }
			set { _AppId = value; }
		}

		/// <summary>
		/// Gets or sets whether the logger should log extended data.
		/// Defaults to false.
		/// </summary>
		public bool ExtendedData { get; set; }

		#endregion

		protected override void Append(LoggingEvent loggingEvent)
		{
			try
			{
				if (_Model == null)
					StartConnection();
			}
			catch (Exception e)
			{
				ErrorHandler.Error("Could not start connection", e);
			}

			var basicProperties = GetBasicProperties(loggingEvent);
			var message = GetMessage(loggingEvent);

			_Model.BasicPublish(_Exchange,
			                    string.Format(_Topic, loggingEvent.Level.Name),
			                    true, false, basicProperties,
			                    message);
		}

		private IBasicProperties GetBasicProperties(LoggingEvent loggingEvent)
		{
			var basicProperties = _Model.CreateBasicProperties();
			basicProperties.ContentEncoding = "utf8";
			basicProperties.ContentType = "text/plain";
			basicProperties.AppId = AppId ?? loggingEvent.LoggerName;

			basicProperties.Timestamp = new AmqpTimestamp(
				Convert.ToInt64((loggingEvent.TimeStamp - _Epoch).TotalSeconds));

			// support Validated User-ID (see http://www.rabbitmq.com/extensions.html)
			basicProperties.UserId = UserName;

			if (ExtendedData)
			{
				basicProperties.Headers["ClassName"] = loggingEvent.LocationInformation.ClassName;
				basicProperties.Headers["FileName"] = loggingEvent.LocationInformation.FileName;
				basicProperties.Headers["MethodName"] = loggingEvent.LocationInformation.MethodName;
				basicProperties.Headers["LineNumber"] = loggingEvent.LocationInformation.LineNumber;
			}

			return basicProperties;
		}


		private byte[] GetMessage(LoggingEvent loggingEvent)
		{
			var sb = new StringBuilder();
			using (var sr = new StringWriter(sb))
			{
				Layout.Format(sr, loggingEvent);

				if (loggingEvent.ExceptionObject != null)
					sr.Write(loggingEvent.GetExceptionString());

				return _Encoding.GetBytes(sr.ToString());
			}
		}

		#region StartUp and ShutDown

		public override void ActivateOptions()
		{
			base.ActivateOptions();
			StartConnection();
		}

		private void StartConnection()
		{
			try
			{
				_Connection = GetConnectionFac().CreateConnection();
				_Connection.ConnectionShutdown += ShutdownAmqp;

				try { _Model = _Connection.CreateModel(); }
				catch (Exception e)
				{
					ErrorHandler.Error("could not create model", e);
				}
			}
			catch (Exception e)
			{
				ErrorHandler.Error("could not connect to Rabbit instance", e);
			}

			if (_Model != null)
				_Model.ExchangeDeclare(_Exchange, ExchangeType.Topic);
		}

		private ConnectionFactory GetConnectionFac()
		{
			return new ConnectionFactory
			{
				HostName = HostName,
				VirtualHost = VHost,
				UserName = UserName,
				Password = Password,
				RequestedHeartbeat = 60,
				Port = (int)Port
			};
		}

		protected override void OnClose()
		{
			base.OnClose();

			ShutdownAmqp(_Connection,
			             new ShutdownEventArgs(ShutdownInitiator.Application, Constants.ReplySuccess, "closing appender"));
		}

		private void ShutdownAmqp(IConnection connection, ShutdownEventArgs reason)
		{
			try
			{
				if (connection != null)
				{
					connection.ConnectionShutdown -= ShutdownAmqp;
					connection.AutoClose = true;
				}

				if (_Model != null)
				{
					_Model.Close(Constants.ReplySuccess, "closing rabbitmq appender, shutting down logging");
					_Model.Dispose();
				}
			}
			catch (Exception e)
			{
				ErrorHandler.Error("could not close model", e);
			}

			_Connection = null;
			_Model = null;
		}

		#endregion
	}
}