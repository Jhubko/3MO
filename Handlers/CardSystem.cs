namespace Discord_Bot.other
{
    internal class CardSystem
    {
        private string[] cardNumbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Walet", "Dama", "Król", "As" };
        private string[] cardSuits = { "Pik", "Trefl", "Kier", "Karo" };

        public int suitIndex { get; set; }
        public int numberIndex { get; set; }
        public string SelectedCard { get; set; }

        public CardSystem()
        {
            var random = new Random();
            numberIndex = random.Next(0, cardNumbers.Length - 1);
            suitIndex = random.Next(0, cardSuits.Length - 1);

            this.SelectedCard = $"{cardNumbers[numberIndex]} {cardSuits[suitIndex]}";
        }

    }
}
