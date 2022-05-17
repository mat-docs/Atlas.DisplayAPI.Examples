<img src="/images/malogo.png" width="300" align="right" /><br><br><br>

# McLaren Applied **ATLAS Display API Sample Code**.

Collection of example code and best practices to use the ATLAS Display API.

The ATLAS Display API facilitates the creation of bespoke ATLAS plugins.

ATLAS Display API is available as a Nuget package to registered users from the **[McLaren Applied Nuget Repository](https://github.com/mat-docs/packages)**.

See the [API Documentation](https://mat-docs.github.io/)

## Notes on upgrading Atlas.DisplayAPI package from versions prior to 11.1.2.344

Version 11.1.2.344 of Atlas.DisplayAPI upgrades System.Reactive from 3.1.1 to 4.4.1. If you are developing your solution in Visual Studio then be advised that the Nuget upgrade process does not automatically remove any previously dependent packages that are no longer required. 
In order to reduce the likelihood of runtime errors we strongly recommend you manually remove the following redundant package following the upgrade:

1. System.Reactive.Interfaces 3.1.1