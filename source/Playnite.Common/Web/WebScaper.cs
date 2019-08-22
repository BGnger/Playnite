using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Playnite.SDK.Models;

namespace Playnite.Common.Web
{
    class WebScaper
    {
        public static List<Game> SearchForGame(string searchTerm)
        {
            var results = new List<Game>();
            results.Add(new Game()
            {
                Name = "Far Cry 4",
                Description = "Go to a rogue nation and shoot people I guess, idk",
                IsStoreItem = true,
                PlayAction = new GameAction()
                {
                    Type = GameActionType.URL,
                    Store = "Steam",
                    Price = "$29.99",
                    Path = "https://store.steampowered.com/app/298110/Far_Cry_4/"
                },
                OtherActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>
                {
                    new GameAction()
                    {
                        Type = GameActionType.URL,
                        Store = "Origin",
                        Price = "$29.99",
                        Path = "https://www.origin.com/usa/en-us/store/far-cry/far-cry-4"
                    },
                     new GameAction()
                    {
                        Type = GameActionType.URL,
                        Store = "Uplay",
                        Price = "$29.99",
                        Path = "https://store.ubi.com/us/far-cry-4/56c4947a88a7e300458b45e2.html?lang=en_US"
                    }
                }
            });
            return results;
        }

        private static HtmlWeb web = new HtmlWeb();
        private static List<Game> SearchSteam(string term)
        {
            var games = new List<Game>();
            var html = @"https://store.steampowered.com/search/?term=" + term.Replace(" ", "%20");
            var htmlDoc = web.Load(html);
            var results = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"search_result_container\"]/div[2]");
            foreach(var result  in results.ChildNodes)
            {
                games.Add(new Game
                {

                });
            }


            return null;
        }


    }
}
