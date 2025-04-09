namespace Discord_Bot.other
{
    internal class CardSystem
    {
        private readonly string[] cardNumbers = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Walet", "Dama", "Król", "As"];
        private readonly string[] cardSuits = ["Pik", "Trefl", "Kier", "Karo"];

        public int SuitIndex { get; set; }
        public int NumberIndex { get; set; }
        public string SelectedCard { get; set; }

        public CardSystem()
        {
            var random = new Random();
            NumberIndex = random.Next(0, cardNumbers.Length - 1);
            SuitIndex = random.Next(0, cardSuits.Length - 1);

            this.SelectedCard = $"{cardNumbers[NumberIndex]} {cardSuits[SuitIndex]}";
        }

    }
}
