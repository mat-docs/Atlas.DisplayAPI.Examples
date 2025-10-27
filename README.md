<img src="/images/malogo.png" width="300" align="right" /><br><br><br>

# McLaren Applied **ATLAS Display API Sample Code**.

Collection of example code and best practices to use the ATLAS Display API.

The ATLAS Display API facilitates the creation of bespoke ATLAS plugins.

ATLAS Display API is available as a Nuget package to registered users from the **[McLaren Applied Nuget Repository](https://github.com/mat-docs/packages)**.

See the [API Documentation](https://mat-docs.github.io/)

## .NET 8 support from version TBC 
Version TBC of `Atlas.DisplayAPI` will be the first release to officially support .NET 8. While plugins built for .NET 6 are expected to remain compatible, we recommend upgrading to .NET 8 to benefit from performance improvements, support, and enhanced features.

## .NET 6 support from version 11.2.3.460
Version 11.2.3.460 of `Atlas.DisplayAPI` will be the first release that only targets .NET 6; we will no longer support .NET Framework integrations from this point onwards. If you still require .NET Framework compatiable builds then please use an older version from the Repository.

## Notes on upgrading Atlas.DisplayAPI package from versions prior to 11.1.2.344

Version 11.1.2.344 of `Atlas.DisplayAPI` upgrades `System.Reactive` from `3.1.1` to `4.4.1`. If you are developing your solution in Visual Studio then be advised that the Nuget upgrade process does not automatically remove any previously dependent packages that are no longer required. 
In order to reduce the likelihood of runtime errors we strongly recommend you manually remove the following redundant package following the upgrade:

1. `System.Reactive.Interfaces` `3.1.1`