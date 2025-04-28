# Hazard!
## Architecture

Note: I took on this demonstration project ***as if* it were to be extended and worked on by teams** in a modern development environment. Smart, incremental development was the goal. This is definitely *not* the simplest way you could develop a board game!
   
## External Dependencies
*Hazard!* relies on the following packages:

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
Around 50 MSTest unit tests help check up on the functioning of crucial systems like the Data Access Layer (AssetFetcher, AssetFactory, DataProvider, and converters), the Registry (TypeRegister and its initializer), and the Binary Serializer. This latter one was a pleasure to work on because I lighted on the "round-trip" test pattern quite on my own!

Tests should be enabled once you have the complete source code in an IDE and you install the Microsoft.Net.Test.Sdk v 17.11, the MSTest.TestFramework v 3.5.2, and the MSTest.TestAdapter v3.5.2 (available via nuget). The tests are in their own separate project (.csproj): Model.UnitTests.

## Copyright Information
All source code © Joshua McKnight, 2024. All rights reserved.  
Artwork © Kiah Baxter-Ferguson and Joshua McKnight, 2024. All rights reserved.

*Hazard!* emulates rules from *Risk: The Game of Global Domination* by Hasbro, Inc.
*Risk: The Game of Global Domination* is © 2020 Hasbro, Inc. All Rights Reserved.

This project is for educational and demonstration purposes only and is not intended for wide distribution or commercial use.


