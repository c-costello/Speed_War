using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpeedWar.Models;
using SpeedWar.Models.Interfaces;

namespace SpeedWar.Pages.Play
{
    public class IndexModel : PageModel
    {
        private IDeckCardManager _deckCardContext;
        

        public IndexModel(IDeckCardManager deckCardManager)
        {
            _deckCardContext = deckCardManager;
        }


        public void OnGet()
        {
        }
        
        /// <summary>
        /// Takes in the player's user ID. Find's the first card in that player's deck. Changes that card's location to the discard pile. Updates the card.
        /// </summary>
        /// <param name="userID">the id of the user playing</param>
        public async void OnPostFlip(int userID)
        {
            DeckCard deckCard = await _deckCardContext.GetCard(userID);
            if (deckCard != null)
            {
                deckCard.DeckID = 1;
                await _deckCardContext.UpdateDeckCard(deckCard);
            }
            else
            {
                EndGame("Computer");
            }
        }

        /// <summary>
        /// Find's the first card in the computer's deck. Changes that cards location to the discard pile. Updates the card.
        /// </summary>
        public async void ComputerFlip()
        {
            DeckCard deckCard = await _deckCardContext.GetCard(2);
            if (deckCard != null)
            {
                deckCard.DeckID = 1;
                await _deckCardContext.UpdateDeckCard(deckCard);
            }
            else
            {
                EndGame("Computer");
            }
        }

        private void EndGame(string v)
        {
            throw new NotImplementedException();
        }
    }
}