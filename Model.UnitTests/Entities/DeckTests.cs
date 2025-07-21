using Model.Entities;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;
using System.Diagnostics;

namespace Model.Tests.Entities
{
    [TestClass]
    public class DeckTests
    {
        private List<MockCard>? _testCards;
        private readonly ICardSet<MockTerrID> _mockSet = new MockCardSet();
        private readonly int _numCards = 50;

        [TestInitialize]
        public void Setup()
        {
            _testCards = [];
            for (int i = 0; i < _numCards; i++)
            {
                _testCards.Add(new(_mockSet));
            }
        }

        private MockDeck InitTestDeck(int lib)
        {
            return InitTestDeck(lib, 0);
        }
        private MockDeck InitTestDeck(int lib, int discard)
        {
            MockDeck newDeck = new();
            if (lib + discard > _testCards!.Count)
                throw new Exception("The combined number of library and discard cards cannot exceed the number of test cards.");

            newDeck.Library = [];
            for (int i = 0; i < lib; i++)
                newDeck.Library.Add(_testCards[i]);

            newDeck.DiscardPile = [];
            for (int i = 0; i < discard; i++)
                newDeck.DiscardPile.Add(_testCards[i]);

            return newDeck;
        }

        [TestMethod]
        public void Shuffle_IsCalled_LibraryRandomized()
        {
            MockDeck testDeck = InitTestDeck(_numCards);
            Assert.IsNotNull(testDeck);
            Assert.IsNotNull(testDeck.Library);
            Assert.IsNotNull(testDeck.DiscardPile);

            int numShuffles = 100000; // ~100 000 000 will get you under varianceLimit of .0001, but takes 3 mins+ on a single thread
            float targetPercentage = 1.00000f / _numCards; // how often a perfect shuffler would place a given card (of the total numcards) in a given library position
            float varianceLimit = 0.01f; // how much the resulting frequencies may differ from ideal to pass
            float[][] cardResults = new float[_numCards][]; // results table in frequencies
            for (int i = 0; i < cardResults.Length; i++)
            {
                cardResults[i] = new float[_numCards];
                for (int j = 0; j < _numCards; j++)
                    cardResults[i][j] = 0;
            }

            Dictionary<string, int> cardIDMap = [];

            /* For each Mock Card, we want a table that records the number of times it appears at a given Library position
             * after an arbitray number of shuffles. This will allow us to test the randomness of the Shuffler. */
            int[][] drawResultsTable = new int[_numCards][]; // Rows for each Card
            for (int numCard = 0; numCard < _numCards; numCard++)
            {
                drawResultsTable[numCard] = new int[_numCards]; // Library Positions
                for (int numPositions = 0; numPositions < _numCards; numPositions++) // Initialize table to 0
                    drawResultsTable[numCard][numPositions] = 0;

                cardIDMap.Add(((MockCard)testDeck.Library[numCard]).ID, numCard); // Slot a row in the table for each unique Card
            }

            for (int i = 0; i < numShuffles; i++)
            {
                testDeck.Shuffle();

                for (int position = 0; position < _numCards; position++)
                {
                    string cardID = ((MockCard)testDeck.Library[position]).ID; // discover which Card is at position
                    int tableRow = cardIDMap[cardID]; // Find the row in the table mapped to this Card numID
                    drawResultsTable[tableRow][position]++; // Increment the count for this library position
                }
            }

            /* After our Shuffling and recording Library Positions, now we view results and compare them
             * against acceptable randomness ranges. */

            Debug.WriteLine($"Target Frequency: {targetPercentage}.");
            foreach (var key in cardIDMap.Keys)
            {
                Debug.WriteLine($"  - Card {key} -    Position : Freq");

                for (int i = 0; i < _numCards; i++)
                {
                    var result = drawResultsTable[cardIDMap[key]][i] / (float)numShuffles;
                    cardResults[cardIDMap[key]][i] = result;
                    Debug.WriteLine($"                               {i} | {result:F5}");
                }
            }

            for (int i = 0; i < cardResults.Length; i++)
            {
                for (int j = 0; j < cardResults[i].Length; j++)
                {
                    var difference = Math.Abs(targetPercentage - cardResults[i][j]);
                    Assert.IsTrue(difference < varianceLimit);
                }
            }
        }
        [TestMethod]
        public void Shuffle_DiscardEmpty_DiscardRemainsEmpty()
        {
            MockDeck testDeck = InitTestDeck(_numCards);
            Assert.IsNotNull(testDeck);
            Assert.IsNotNull(testDeck.Library);
            Assert.IsNotNull(testDeck.DiscardPile);
            testDeck.Shuffle();
            Assert.AreEqual(0, testDeck.DiscardPile.Count);
        }
        [TestMethod]
        public void Shuffle_IsCalledWithDiscards_LibraryTakesDiscards()
        {
            MockDeck testDeck = InitTestDeck(_numCards - 10, 10);
            Assert.IsNotNull(testDeck);
            Assert.IsNotNull(testDeck.Library);
            Assert.IsNotNull(testDeck.DiscardPile);
            int numLibrary = testDeck.Library.Count;
            int numDiscards = testDeck.DiscardPile.Count;
            string[] discardIDs = [.. testDeck.DiscardPile
                .Select(card => (MockCard)card)
                .Select(card => card.ID)];

            testDeck.Shuffle();

            Assert.AreEqual(numLibrary + numDiscards, testDeck.Library.Count);
            Assert.AreEqual(0, testDeck.DiscardPile.Count);
            foreach (var id in discardIDs)
                Assert.IsTrue(testDeck.Library.Where(item => ((MockCard)item).ID == id).Any());
        }
        [TestMethod]
        public void DrawCard_IsCalled_BottomLibCardReturned()
        {
            MockDeck testDeck = InitTestDeck(_numCards);
            Assert.IsNotNull(testDeck);
            Assert.IsNotNull(testDeck.Library);
            Assert.IsNotNull(testDeck.DiscardPile);
            int numLib = testDeck.Library.Count;
            var bottomCardID = ((MockCard)testDeck.Library[^1]).ID;

            var drawnCard = testDeck.DrawCard();

            Assert.AreEqual(numLib - 1, testDeck.Library.Count);
            Assert.AreEqual(bottomCardID, ((MockCard)drawnCard).ID);
        }
        [TestMethod]
        public void DrawCard_EmptyLibrary_LibRenewedAndDrawnFrom()
        {
            MockDeck testDeck = InitTestDeck(0, _numCards);
            Assert.IsNotNull(testDeck);
            Assert.IsNotNull(testDeck.Library);
            Assert.IsNotNull(testDeck.DiscardPile);
            int numDiscards = testDeck.DiscardPile.Count;
            string aDiscardID = ((MockCard)testDeck.DiscardPile[numDiscards - 1]).ID;

            var drawnCard = testDeck.DrawCard();
            MockCard mockCard = (MockCard)drawnCard;

            Assert.AreEqual(numDiscards - 1, testDeck.Library.Count);
            Assert.AreEqual(0, testDeck.DiscardPile.Count);
            bool testIDinLibrary = testDeck.Library.Where(card => ((MockCard)card).ID == aDiscardID).Any();
            Assert.IsTrue(testIDinLibrary || mockCard.ID == aDiscardID);
            if (testIDinLibrary)
                Assert.AreNotEqual(aDiscardID, mockCard.ID);
        }
    }
}