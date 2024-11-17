# Hazard!
*Hazard!* is a working "hot-seat" board game playable by two to six players based on the popular board game *Risk* (owned by Hasbro, Inc). Its source code is written entirely in C#, with the exception of the WPF UI which also relies on XAML.

## Purpose
As a portfolio project, Hazard has two primary purposes:
  1. To force learning on me, its sole developer.
  2. To demonstrate those learned skills.

As a corrollary, it also aims at contemporary, professional industry code standards for
1. Writing
2. Testing
3. Documentation, and
4. Organization/Architecture.
   
## Over-Engineering
Both primary purposes (1) and (2) above motivate intentional *over-engineering.* I took on the project ***as if* it were to be extended and worked on by teams** in a modern development environment. Smart, incremental development is the intended approach.

See the [Architecture](#architecture) and [Feature Highlights](#feature-highlights) sections for more details. 

If the project were designed to be what it actually is -- a solo desktop application written in WPF with very little chance of extension -- it could be written much more simply. But that would be another project (one I may or may not undertake).

## Background
I had a Visual Basic project from a high school programming course that I never got fully working. It was to emulate Hasbro's *Risk*. I decided to achieve that long abandoned goal, but updated to use modern languages, frameworks, and other technologies.

Discovering which languages and tools would be used today, I landed on Windows Presentation Foundation (WPF). Fortunately, Microsoft's C# and the .NET ecosystem have developed a great deal. In retrospect, this decision did narrow my initial focus to desktop development, but I also discovered that MVVM was widely used in web contexts as well (and much more so, it's close cousin MVC), so natural progress might lead to working in a web context in the medium to long term.

## Architecture
*Hazard!*'s design follows the Model-View-Viewmodel(MVVM) pattern. 

Basically, this means that the game simulation (state and rules logic) is handled on the lowest, "Model" layer, the player interacts with a UI at the highest, "View" layer, and a "ViewModel" layer mediates between them. Often, as here, this means relying heavily on the Observer pattern, with bindings and events being central to the working structure of the application.

Each layer -- Model, View, and ViewModel -- is encapsulated in its own Project (.csproj). There is a Shared Project referenced at the Model layer, including interfaces, global variables, and application services.

Project/Layer references are asymmetrical, with "higher" layers referring to ("knowing about") lower layers, but not vice versa. That is, the Model has no references (beyond the Shared Project), the ViewModel references the Model, and the View references the ViewModel. The relations can be depicted like so: Model <- ViewModel <- View , with each arrow representing a reference.

In this case, there is one caveat: The start-up project for a WPF application could be separated from the three layers, but as is sometimes done for convenience, it is incorporated into the "View" Project for this solution because the DI system is housed in App.xaml.cs. This means the View technically references the Model, but only because the start-up process requires references to Shared interface definitions.

## Feature Highlights
A few systems involved more effort or met emerging needs in interesting or satisfying ways (associated namespace parenthesized):

1. A class Registry which registers classes and associated objects, like string names, data converters, etc. Combined with reflection, enables runtime operations for, e.g., loading assets. (Shared.Services.Registry)
2. Default methods on the card interface leverage reflection to enable rapid proto-typing of ICard implementations. (Shared.Interfaces.Model.ICard)
3. A serializer class, BinarySerializer, that encodes, writes, and reads IConvertibles of any object implementing IBinarySerializable (Shared.Services.Serializer).
4. A fully functional UI card system with visuals, trading, and a sensitivity to "hot-seat" requirements -- e.g., card fronts only visibile to current player. (View)
5. Unit Tests for the Data Access Layer, Registry, and Binary Serializer, as well a fun one for the Deck.Shuffle() method which assures that it is shuffling properly! These are not part of the Release, but are available in the source code under **Hazard_Model.UnitTests**. See [the staging tests section to get them running.](#staging-unit-tests)

## Notable Features
At specific layers, less intensive but still crucial or highly instructive systems include (namespace again parenthesized):

**Model**
1. Data Access Layer ties into the Application Registry and automatically loads '.json' game assets. (Model.DataAccess)
2. Automatic Board setup for 2-player games leverages a BitArray and bitwise manipulators (in lieu of bool[]). (Core.Game.TwoPlayerAutoSetup())
3. An implementation of the Fischer-Yates shuffle algorithm. (Entities.Deck.Shuffle())

**View**
1. Territories are custom FrameworkElements whose visuals are determined at runtime, enabling easy extension. (TerritoryElement, MainWindow.BuildTerritoryButtons())
2. Attacking and other player actions are accompanied by truly random dice-rolling, feedback animations, and hot-keys which allow for responsive and legible play. (AttackWindow, MainWindow.xaml)
3. Responsive and illustrative application commands and windows, complete with keyboard short-cuts. (NewGameWindow, MainWindow.xaml InputBindings>
4. Game State changes reflected in game rule notices/hints, and the information highlighted in Player data boxes. (MainWindow.xaml Resources, .BuildPlayerDataBoxes())
5. An example of overengineering as learning, but still cool: Player data boxes are generated programatically (MainWindow.BuildPlayerDataBoxes())
   
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

Tests should be enabled once you have the complete source code in an IDE and you install the Microsoft.Net.Test.Sdk v 17.11, the MSTest.TestFramework v 3.5.2, and the MSTest.TestAdapter v3.5.2 (available via nuget). The tests are in their own separate project (.csproj): Hazard_Model.UnitTests.

## Short Term Improvements
If it is a good use of time for *learning*, these would be the next steps:
1. Implement the 'Secret Mission' game option, complete with new Card type and win conditions.

## Medium Term Possibilities
Some bigger, medium-term extensions could lead to future features like:
1. Multi-player support via TCP-IP, etc.
2. A local queried archive (MySql) containing error/event logs, game data records, save files(?), or other information gathered across multiple games.
## Long Term Ideas
Here are some possible long-term paths:
1. Cross-Platform via Avalonia or .NET Maui port
2. Online Game Server enabling multiplayer via "lobby" or direct invite.
3. After (2), back the Server with a database storing player account info, game records, and the like, enabling leaderboards, achievements, etc.

## Copyright Information
All source code © Joshua McKnight, 2024. All rights reserved.  
Artwork © Kiah Baxter-Ferguson and Joshua McKnight, 2024. All rights reserved.

*Hazard!* emulates basic game mechanics and rules from *Risk: The Game of Global Domination* by Hasbro, Inc.
*Risk: The Game of Global Domination* is © 2020 Hasbro, Inc. All Rights Reserved.

This project is for educational and demonstration purposes only and is not intended for wide distribution or commercial use.


