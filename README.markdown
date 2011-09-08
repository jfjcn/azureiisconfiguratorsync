Using ServerManager in the development favric without breaking IISConfigurator.exe
==================================================================================

This code sets the application pool identity of your web roles to an identity stored in 
the registry, and syncronizes initialization of Windows Azure web roles in the 
development fabric, to prevent race conditions with IISConfigurator.exe...

Registry settings for setting application pool identity
-------------------------------------------------------

	Windows Registry Editor Version 5.00
	
	[HKEY_CURRENT_USER\Software\Microsoft EMIC\Cloud\VENUS-C]
	"DomainUserName"="EUROPE\\chgeuer"
	"DomainPassword"="PASSWORD"

In your web role, you basically call 

	public class WebRole : RoleEntryPoint
	{
		public override bool OnStart()
		{
			WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer.ServerManagerBarrier.TweakIdentityWhenRunningInCorpnet();
			
			return base.OnStart();
		}
	}

Further links
-------------

http://www.wadewegner.com/2011/01/programmatically-changing-the-apppool-identity-in-a-windows-azure-web-role/#comment-4251

