# Hazard!
## Details
For full details: https://livingcryogen.github.io/Hazard/.

Note: I took on this demonstration project ***as if* it were to be extended and worked on by teams** in a modern development environment. Smart, incremental development was the goal. (This is definitely *not* the simplest way you could develop a board game!)
   
## External Dependencies
*Hazard!*'s core relies on the following packages:

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

## Copyright Information
All source code © Joshua McKnight, 2024. All rights reserved.  
Artwork © Kiah Baxter-Ferguson and Joshua McKnight, 2024. All rights reserved.

*Hazard!* emulates rules from *Risk: The Game of Global Domination* by Hasbro, Inc.
*Risk: The Game of Global Domination* is © 2020 Hasbro, Inc. All Rights Reserved.

This project is for educational and demonstration purposes only and is not intended for wide distribution or commercial use.


