﻿using Microsoft.EntityFrameworkCore;
using SpeedWar.Data;
using SpeedWar.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeedWar.Models.Services
{
    public class DeckCardMgmtSvc : IDeckCardManager
    {
        private CardDbContext _context;

        public DeckCardMgmtSvc(CardDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// collects and returns a specified deck as a list
        /// </summary>
        /// <param name="userID"> deck owner's ID </param>
        /// <param name="deckType"> deck type (ie - 'play', 'collect', 'discard' </param>
        /// <returns> card deck, formatted as list </returns>
        public async Task<List<DeckCard>> GetDeck(int userID, DeckType deckType)
        {
            Deck deck = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == userID && d.DeckType == deckType);
            List<DeckCard> cardDeck = await _context.DeckCards.Where(d => d.DeckID == deck.ID).ToListAsync();
            return cardDeck;
        }

        /// <summary>
        /// returns a random card from specified deck
        /// simulates returning top card from a shuffled deck
        /// </summary>
        /// <param name="userID"> deck owner's ID </param>
        /// <param name="deckType"> deck type (ie - 'play', 'collect', 'discard' </param>
        /// <returns> randomly selected card from specified deck </returns>
        public async Task<DeckCard> GetCard(int userID, DeckType deckType)
        {
            List<DeckCard> cardDeck = await GetDeck(userID, deckType);
            Random random = new Random();
            int rnd = random.Next(0, cardDeck.Count - 1);
            DeckCard deckCard = cardDeck[rnd];
            return deckCard;
        }

        /// <summary>
        /// updates the deck-location of a card
        /// </summary>
        /// <param name="deckCard"> new card-deck assignment </param>
        /// <returns> completed task </returns>
        public async Task UpdateDeckCard(int cardID, int oldDeckID, int newDeckID)
        {

            var query = await _context.DeckCards.FirstOrDefaultAsync(d => d.CardID == cardID && d.DeckID == oldDeckID);
            await Task.Run(() => _context.DeckCards.Remove(query));

            DeckCard deckCard = new DeckCard()
            {
                CardID = cardID,
                DeckID = newDeckID
            };
            await _context.DeckCards.AddAsync(deckCard);
            await _context.SaveChangesAsync();
        
        }

        /// <summary>
        /// gets all cards and returns as a list
        /// </summary>
        /// <returns> list of all cards </returns>
        public async Task<List<Card>> GetAllCardsAsync()
        {
            return await _context.Cards.ToListAsync();
        }

        /// <summary>
        /// 'deals' a new game by splitting all cards randomly between player and computer
        /// </summary>
        /// <param name="ID"> player's UserID </param>
        /// <returns> task completed </returns>
        public async Task DealGameAsync(int ID)
        {
            Deck player = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == ID && d.DeckType == DeckType.Play);
            Deck playerColl = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == ID && d.DeckType == DeckType.Collect);
            Deck computer = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == 2 && d.DeckType == DeckType.Play);
            Deck computerColl = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == 2 && d.DeckType == DeckType.Collect);
            Deck discard = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == 1 && d.DeckType == DeckType.Discard);
            await CleanDeck(player);
            await CleanDeck(computer);
            await CleanDeck(discard);
            await CleanDeck(playerColl);
            await CleanDeck(computerColl);
            await DealGameAsync(player, computer);
        }

        /// <summary>
        /// (HELPER / overload)
        /// Deals cards to Player and Computer 'Play' decks
        /// </summary>
        /// <param name="player"> client player's 'Play' deck </param>
        /// <param name="computer"> computer player's 'Play' deck </param>
        /// <returns> completed task </returns>
        private async Task DealGameAsync(Deck player, Deck computer)
        {
            List<Card> cards = await GetAllCardsAsync();
            cards.Remove(cards.FirstOrDefault(c => c.ID == 53));
            cards.Remove(cards.FirstOrDefault(c => c.ID == 54));
            Random random = new Random();
            int rnd;
            Deck current = player;
            while (cards.Count > 0)
            {
                rnd = random.Next(0, cards.Count - 1);
                await _context.DeckCards.AddAsync(new DeckCard() { CardID = cards[rnd].ID, DeckID = current.ID });
                await _context.SaveChangesAsync();
                DeckCard test = await _context.DeckCards.FirstOrDefaultAsync(c => c.CardID == cards[rnd].ID && c.DeckID == current.ID);
                cards.Remove(cards[rnd]);
                current = (current == player) ? computer : player;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Takes in a deck, clears the cards out of the deck.
        /// </summary>
        /// <param name="deck">Any Deck</param>
        /// <returns>No return, saves changes</returns>
        public async Task CleanDeck(Deck deck)
        {
            var cards = await GetDeck(deck.UserID, deck.DeckType);
            foreach (var card in cards)
            {
                _context.DeckCards.Remove(card);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// compares ranks of top 2 cards in discard pile
        /// returns 'true' if matching, returns 'false' if not matching
        /// </summary>
        /// <param name="last"> 2nd card in discard pile </param>
        /// <param name="next"> top card in discard pile </param>
        /// <returns> 'true' if last.Rank matches next.Rank, 'false' otherwise </returns>
        public bool CompareCards(Card last, Card next)
        {
            if (last.Rank == next.Rank)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// plays a random card from selected player's 'Play' deck onto 'Discard' pile
        /// </summary>
        /// <param name="ID"> UserID of player who's playing </param>
        /// <returns> card in play </returns>
        public async Task<Card> Flip(int ID)
        {
            var check = await GetDeck(ID, DeckType.Play);

            // if 'Play' deck isn't empty, then play a random card
            if(check.Count > 0)
            {
                int DeckID = (await _context.Decks.FirstOrDefaultAsync(d => d.UserID == ID && d.DeckType == DeckType.Play)).ID;
                int CardID = (await GetCard(ID, DeckType.Play)).CardID;
                await UpdateDeckCard(CardID, DeckID, 1);
                Card card = await _context.Cards.FirstOrDefaultAsync(c => c.ID == CardID);
                return card;
            }
            // if 'Play' deck is empty, then can't play unless/until successful 'Slap'
            return null;
        }




        /// <summary>
        /// RESET: Moves all cards from specified player's 'Collect' deck to same player's 'Play' deck (used to reset decks when 'Play' deck runs empty)
        /// SLAP:  Moves all cards from Discard deck to specified player's 'Collect' deck  (used when a match occurs and a user 'slaps')
        /// </summary>
        /// <param name="ID"> ID of User who 'slapped' or needs reset </param>
        /// <param name="slap"> indicates whether reset is of type 'slap' </param>
        /// <returns> completed task </returns>
        public async Task ResetDecks(int ID)
        {
            List<DeckCard> donor = await GetDeck(ID, DeckType.Collect);
            Deck recipient = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == ID && d.DeckType == DeckType.Play);
            await ResetDecks(donor, recipient);
        }

        /// <summary>
        /// reassigns all cards in 'Discard' deck to specified player's 'Collect' deck after verified 'Slap'
        /// </summary>
        /// <param name="ID"> UserID of player with verified 'slap' </param>
        /// <returns> completed task </returns>
        public async Task Slap(string username)
        {
            User slapper = await _context.Users.FirstOrDefaultAsync(u => u.Name == username);
            List<DeckCard> donor = await GetDeck(1, DeckType.Discard);
            Deck recipient = await _context.Decks.FirstOrDefaultAsync(d => d.UserID == slapper.ID && d.DeckType == DeckType.Collect);
            await ResetDecks(donor, recipient);
        }

        /// <summary>
        /// (HELPER for public ResetDecks() and Slap() methods)
        /// reassigns all cards in specified donor deck to specified recipient deck
        /// </summary>
        /// <param name="donor"> deck from which cards are to be moved </param>
        /// <param name="recipient"> deck to which cards are to be moved </param>
        /// <returns> completed task </returns>
        private async Task ResetDecks(List<DeckCard> donor, Deck recipient)
        {
            DeckCard temp = new DeckCard();
            foreach (DeckCard card in donor)
            {
                temp.CardID = card.CardID;
                await UpdateDeckCard(card.CardID, card.DeckID, recipient.ID);
            }
        }

        /// <summary>
        /// checks to see whether either user has run out of cards and declares the other user as winner
        /// </summary>
        /// <param name="user"> client player </param>
        /// <returns> user declared 'winner', or null if game continues </returns>
        public async Task<User> CheckWinner(int ID)
        {
            User player = await _context.Users.FindAsync(ID);
            User comp = await _context.Users.FindAsync(2);
            if ( await EmptyDecks(2) )
            { return player; };
            if (await EmptyDecks(ID))
            { return comp; };
            return null;
        }
        /// <summary>
        /// check if the deck is empty
        /// </summary>
        /// <param name="ID">user's ID</param>
        /// <returns>boolean true if it is empty</returns>
        public async Task<bool> EmptyDecks(int ID)
        {
            List<DeckCard> playUser = await GetDeck(ID, DeckType.Play);
            List<DeckCard> collectUser = await GetDeck(ID, DeckType.Collect);
            if (playUser.Count == 0 && collectUser.Count == 0)
            { return true; };
            return false;
        }
    }
}
