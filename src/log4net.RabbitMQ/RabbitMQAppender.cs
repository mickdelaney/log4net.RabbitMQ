using System;
using System.Configuration;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using log4net.Appender;
using log4net.Core;

namespace log4net.RabbitMQ
{
	public class RabbitMQAppender : AppenderSkeleton
	{
		private string _Exchange = "log4net-logging";
		private IConnection _Connection;
		private IModel _Model;
		private readonly Encoding _Encoding = Encoding.UTF8;
		private readonly DateTime _Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		#region Properties

		private string _VHost = "/";

		public string VHost
		{
			get { return _VHost; }
			set { if (value != null) _VHost = value; }
		}

		private string _UserName = "guest";

		public string UserName
		{
			get { return _UserName; }
			set { _UserName = value; }
		}

		private string _Password = "guest";

		public string Password
		{
			get { return _Password; }
			set { _Password = value; }
		}

		private uint _Port = 5672;

		public uint Port
		{
			get { return _Port; }
			set { _Port = value; }
		}

		private string _Topic = "{0}";

		/// <summary>
		/// 	Gets or sets the routing key (aka. topic) with which
		/// 	to send messages. Defaults to {0}, which in the end is 'error' for log.Error("..."), and
		/// 	so on. An example could be setting this property to 'ApplicationType.MyApp.Web.{0}'
		/// </summary>
		public string Topic
		{
			get { return _Topic; }
			set { _Topic = value; }
		}

		private IProtocol _Protocol = Protocols.DefaultProtocol;

		public IProtocol Protocol
		{
			get { return _Protocol; }
			set { if (value != null) _Protocol = value; }
		}

		/// <summary>
		/// 	Sets the protocol from a string.
		/// 	Uses <see cref = "Protocols.Lookup" /> internally.
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

			var basicProperties = _Model.CreateBasicProperties();
			basicProperties.ContentEncoding = "utf8";
			basicProperties.ContentType = "text/plain";
			basicProperties.AppId = loggingEvent.Domain;
			basicProperties.Timestamp = new AmqpTimestamp(
				Convert.ToInt64((loggingEvent.TimeStamp - _Epoch).TotalSeconds));

			var message = GetMessage(loggingEvent);
			_Model.BasicPublish(_Exchange,
			                    string.Format(_Topic, loggingEvent.Level.Name),
			                    true, false, basicProperties,
			                    message
				);
		}

		private byte[] GetMessage(LoggingEvent loggingEvent)
		{
			string ex = loggingEvent.GetExceptionString();

			var sb = new StringBuilder(loggingEvent.RenderedMessage, 
				loggingEvent.RenderedMessage.Length
				+ (ex == null 
					? 0 
					: ex.Length + Environment.NewLine.Length));

			if (ex != null)
			{
				sb.Append(Environment.NewLine);
				sb.Append(ex);
			}

			return _Encoding.GetBytes(sb.ToString());
		}

		#region StartUp and ShutDown

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
				RequestedHeartbeat = 60
			};
		}

		#endregion

	}
}