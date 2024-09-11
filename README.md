# Hazard!
*Hazard!* is a working "hot-seat" (local only) board game playable by two to six players based on the popular board game *Risk* (owned by Hasbro, Inc). Its source code is written entirely in C#, with the exception of the WPF UI which also relies on XAML.

## Purpose
As a software development portfolio project, Hazard has two primary purposes:
  1. To force learning on me, its sole developer.
  2. To demonstrate those learned skills.

As a corrollary, it also aims at contemporary, professional industry standards in the
1. Writing
2. Testing
3. Documentation, and
4. Organization/Architecture of code.

This means it is intentionally "over-engineered": I took on the project ***as if* it were to be extended and worked on by teams** in a modern development environment. Smart, incremental development is the approach.
See the [Architecture](#architecture) and [Feature Highlights](#feature-highlights) sections for more details. 

If anyone has some fun or is charmed by my wife's art, then the project has achieved beyond its goals! :)

## Background
When choosing a first portfolio project, I remembered a high school Visual Basic program I never got fully working which was to emulate Hasbro's *Risk*. I decided to achieve that early goal, but updated to use modern languages, frameworks, and other technologies.

Discovering which languages and tools would be used today, I landed on Windows Presentation Foundation (WPF). Fortunately, Microsoft's C# and the .NET ecosystem has developed a great deal. In retrospect, this decision did narrow the initial focus to desktop development, but I also discovered that MVVM was widely used in web contexts as well (and much more so, it's close cousin MVC), so it appears that the focus might progress naturally to a web context in the medium to long term.
## Architecture
*Hazard!*'s design follows the Model-View-Viewmodel(MVVM) pattern. 

Basically, this means that the game simulation (state and rules logic) is handled on the lowest, "Model" layer, the player interacts with a UI at the highest, "View" layer, and a "ViewModel" layer mediates between them. Often, as here, this means relying heavily on the Observer pattern, with bindings and events being central to the working structure of the application.

Each layer -- Model, View, and ViewModel -- is encapsulated in its own Project (.csproj). There is a Shared Project including interfaces and globals (currently, enums and a registry service) referenced at the Model layer.

Project/Layer references are asymmetrical, with "higher" layers referring to ("knowing about") lower layers, but not vice versa. That is, the Model has no references (beyond the Shared Project), the ViewModel referecences the Model, and the View references the ViewModel. The relations can be depicted like so: Model <- ViewModel <- View , with each arrow representing a reference.

In this case, there is one caveat: The start-up project for a WPF application could be separated from the three layers, but as is sometimes done for convenience, it is incorporated into the "View" Project for this solution because the DI system is housed in App.xaml.cs.

## Feature Highlights
A few systems involved more effort or met emerging needs in interesting or satisfying ways (associated namespace parenthesized):

1. A class Registry which registers classes and associated objects, like string names, data converters, etc. Combined with reflection, enables runtime operations for, e.g., loading assets. (Hazard_Shared.Services.Registry)
2. Default methods on the card interface (ICard) which allows easy use of future ICard implementations with the DAL if properly Registered, and asset files properly structured. (Hazard_Share.Interfaces.Model.ICard)
3. UI card system is sensitive to "hot-seat" requirements -- e.g., cards fronts only visibile to current player (backs otherwise), etc.

## Notable Features
At specific layers, less intensive but still crucial or highly instructive systems include (namespace again parenthesized):
**Model**
1. Data Access Layer ties into the Application Registry and automatically loads '.json' game assets (for now, limited to card data). (.DataAccess)
2. A BinarySerializer for reading and writing binary savefiles. (Core.Game.BinarySerializer)
3. Automatic Board setup for 2-player games leverages a byte and bitwise manipulators (in lieu of bool[]). (Core.Game.TwoPlayerAutoSetup())
4. An implementation of the Fischer-Yates shuffle algorithm. (Entities.Deck.Shuffle())

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

## Short Term Improvements
If it is a good use of time for *learning*, these would be the next steps:
1. Update all game assets to load via '.json' or configuration files to match current ICard/ICardSet functionality and fulfill the promise of the current system. (Board geography, rule values, etc)
2. Implement the 'Secret Mission' game option, complete with new Card type and win conditions.

## Medium Term Possibilities
If short-term improvements are mostly redundant from a learning or demonstrating disposition, some bigger, medium-term extensions could lead to future features like:
1. Multi-player support via TCP-IP, etc.
2. A local queried archive (MySql) containing error/event logs, game data records, save files(?), or other information gathered across multiple games.
## Long Term Ideas
Similarly, if continuing to develop this project further remains a good idea, here are some possible long-term paths:
1. Cross-Platform via Avalonia or .NET Maui port
2. Online Game Server enabling multiplayer via "lobby" or direct invite.
3. After (2), back the Server with a database storing player account info, game records, and the like, enabling leaderboards, achievements, etc.

## Copyright Information
All source code © Joshua McKnight, 2024. All rights reserved.  
Artwork © Kiah Baxter-Ferguson and Joshua McKnight, 2024. All rights reserved.

*Hazard!* emulates basic game mechanics and rules from *Risk: The Game of Global Domination* by Hasbro, Inc.
*Risk: The Game of Global Domination* is © 2020 Hasbro, Inc. All Rights Reserved.


