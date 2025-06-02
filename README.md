# About *Hazard!: Nothing Ventured, Nothing Gained*

*Hazard!* is a software engineering demonstration and functioning board game for (2-6) local players. 
It showcases the entire production process: development, QA (testing and documentation), automated cloud deployment, and secure public distribution.

For full project background and details, including architecture, code, and testing highlights, see: https://livingcryogen.github.io/Hazard/.

Oh, and you there can download and install it as a signed .MSIX package!  :)

## Dependencies
*Hazard!*'s core relies on the following package versions (or newer):

**Third-Party**
1. System.IO.Abstractions v 21.0.29, by Tatham Oddie & friends
2. NHotkey.Wpf v 3.0.0, by Thomas Levesque

**Microsoft**
1. CommunityToolkit.Mvvm v 8.2.2
2. .NET.Test.Sdk v 17.11
3. MSTest.TestFramework v 3.5.2
4. MSTest.TestAdapter v 3.5.2

**Microsoft.Extensions v 8.0**
1. .Hosting
2. .Logging
3. .Configuration
4. .DependencyInjection

## Staging Unit Tests
Around 50 MSTest unit tests verify the functioning of crucial systems.

Tests should be enabled once you have the complete source code of the "Model," "Model.UnitTests," and "Shared" projects in an IDE with Microsoft.Net.Test.Sdk, the MSTest.TestFramework, and the MSTest.TestAdapter installed (available via nuget).


