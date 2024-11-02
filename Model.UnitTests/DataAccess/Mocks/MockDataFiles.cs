using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures;
using Shared.Services.Registry;
using System.Text.Json;

namespace Model.Tests.DataAccess.Mocks;

public class MockDataFiles
{
    private readonly JsonSerializerOptions _jOption = new() { WriteIndented = true };
    public string? CardSetPath { get; private set; }
    public string[] ConfigDataFileList { get; } = [
        (string) SharedRegister.Registry[typeof(MockCardSet)]![RegistryRelation.DataFileName]!,
        ];

    public MockDataFiles()
    {
        BuildCardSetFiles();
    }

    private void BuildCardSetFiles()
    {
        string temp = FileProcessor.GetTempFile();
        CardSetPath = temp + "CardSet.json";
        FileProcessor.Move(temp, CardSetPath);
        BuildMockCardSetJson();

        // Shared. Registry DataFile name must be added for the MockCardSet class
        if (SharedRegister.Registry[typeof(MockCardSet)]![RegistryRelation.DataFileName] == null)
            SharedRegister.Registry.AddRelation(typeof(MockCardSet), (CardSetPath, RegistryRelation.DataFileName));
        else {
            SharedRegister.Registry.RemoveRelation(typeof(MockCardSet), RegistryRelation.DataFileName);
            SharedRegister.Registry.AddRelation(typeof(MockCardSet), (CardSetPath, RegistryRelation.DataFileName));
        }

        ConfigDataFileList[0] = (string)SharedRegister.Registry[typeof(MockCardSet)]![RegistryRelation.DataFileName]!;
    }
    private void BuildMockCardSetJson()
    {
        List<string> mockTerritories = ["Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida",
            "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine",
            "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska",
            "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio",
            "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee",
            "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming"];
        List<string> mockInsignia = ["Marine", "FighterJet", "Tank"];
        var jsonDocument = new
        {
            TroopCards = new object[mockTerritories.Count]
        };

        int insigniaIndex = 0;
        for (int i = 0; i < mockTerritories.Count; i++) {
            var troopCardJObject = new
            {
                Targets = new string[] { mockTerritories[i] },
                Insignia = mockInsignia[insigniaIndex]
            };

            jsonDocument.TroopCards[i] = troopCardJObject;

            if (insigniaIndex >= 2)
                insigniaIndex = 0;
            else
                insigniaIndex++;
        }

        string jsonString = JsonSerializer.Serialize(jsonDocument, _jOption);
        FileProcessor.WriteFile(CardSetPath!, jsonString);
    }

    public static void CleanUp(string filePath)
    {
        FileProcessor.Delete(filePath);
    }
}
