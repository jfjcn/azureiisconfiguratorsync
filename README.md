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

````C#
public class WebRole : RoleEntryPoint
{
	public override bool OnStart()
	{
		WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer.
			ServerManagerBarrier.TweakIdentityWhenRunningInCorpnet();
			
		return base.OnStart();
	}
}
````

Further links
-------------

http://www.wadewegner.com/2011/01/programmatically-changing-the-apppool-identity-in-a-windows-azure-web-role/#comment-4251



Instead of using NuGut, you can also use the T4Include NuGet package like this:

````
<#
    // Whenever this file is saved the files in the Includes section is downloaded
    // from GitHub (you can download from other websources by changing rootpath)
    RootPath    = @"https://raw.github.com/";
    Namespace   = "AzureHelperLibrary"         ;   // The downloaded content is wrapped in this namespace
    Includes    = new []
        {
            Include (@"mrange/T4Include/master/Extensions/BasicExtensions.cs"),
			Include (@"chgeuer/azureiisconfiguratorsync/master/src/WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer/CrossProcessBarrier.cs"),
			Include (@"chgeuer/azureiisconfiguratorsync/master/src/WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer/DevelopmentFabricIdentifiers.cs"),
			Include (@"chgeuer/azureiisconfiguratorsync/master/src/WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer/DevelopmentFabricBarrier.cs"),
			Include (@"chgeuer/azureiisconfiguratorsync/master/src/WindowsAzure.DevelopmentFabric.IISConfigurator.Syncronizer/ServerManagerBarrier.cs"),
        }; 
#>

<#@ include file="$(SolutionDir)\packages\T4Include.1.1.2\T4\IncludeWebFile.ttinclude" #>
````
