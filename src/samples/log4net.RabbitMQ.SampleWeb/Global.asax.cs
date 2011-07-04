using System.IO;
using System.Web.Mvc;
using System.Web.Routing;
using log4net.Config;

namespace log4net.RabbitMQ.SampleWeb
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
		private static readonly ILog _Logger = LogManager.GetLogger(typeof(MvcApplication));
		

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
			);
		}

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
			XmlConfigurator.ConfigureAndWatch(new FileInfo(Server.MapPath("~/log4net.config")));

			RegisterRoutes(RouteTable.Routes);
		}

		// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
		protected void Application_Error()
		{
			var lastError = Server.GetLastError();
			Server.ClearError();

			_Logger.Error("app error", lastError);
		}

		// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
		protected void Application_End()
		{
			_Logger.Info("shutting down application");

			//LogManager.Shutdown();
		}
	}
}