using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Playnite.SDK.Models;

namespace Playnite.Common.Web
{
    public class WebScraper
    {
        private static string GogSearchApiUrl = @"http://embed.gog.com/games/ajax/filtered?limit=10&mediaType=game&search=";

        public static List<Game> SearchForGame(string searchTerm)
        {
            var results = new List<Game>();
            var steamItems = SearchSteam(searchTerm);
            results.AddRange(steamItems);
            results.AddRange(SearchUplay(searchTerm));
            results.AddRange(SearchGOG(searchTerm));
            return SortListOnRelavance(results, searchTerm);
        }

        private static HtmlWeb web = new HtmlWeb();

        private static List<Game> SearchGOG(string searchTerm)
        {
            List<Game> requestValue = new List<Game>();
            var validUrlQuery = searchTerm.Replace(" ", "%20");
            GogSearchApiUrl = $"{GogSearchApiUrl}{validUrlQuery}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GogSearchApiUrl);
            request.Method = "GET";
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var stream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream))
                {
                    JObject gogRequestJson = JObject.Parse(reader.ReadToEnd());
                    foreach (var product in gogRequestJson["products"])
                    {
                        String price = (product["price"]["symbol"].ToString() + product["price"]["amount"].ToString());
                        //price = price.Contains("0.00") ? "Free" : price;
                        Game game = new Game()
                        {
                            Name = product["title"].ToString(),
                            IsStoreItem = true,
                            PlayAction = new GameAction()
                            {
                                Type = GameActionType.URL,
                                Price = price,
                                Store = "GOG",
                                Path = $"http://www.gog.com{product["url"]}"
                            }
                        };
                        requestValue.Add(game);
                    }
                }
            }
            return requestValue;
        }

        private static List<Game> SearchSteam(string term)
        {
            var games = new List<Game>();
            var html = @"https://store.steampowered.com/search/?term=" + term.Replace(" ", "%20") + "&category1=998";
            var htmlDoc = web.Load(html);
            var results = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"search_result_container\"]/div[2]");
            foreach (var result in results.ChildNodes)
            {
                if (result.Name == "a")
                {
                    games.Add(new Game()
                    {
                        Name = result.Descendants("span").First().InnerText,
                        IsStoreItem = true,
                        PlayAction = new GameAction()
                        {
                            Store = "Steam",
                            Path = result.Attributes["href"].Value
                        }
                    });

                }
            }
            return games;
        }

        private static string GetSteamGameInfo(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            //Insert cookie to make Steam think we are old enough to view content (No redirect to age verification)
            Cookie cookie = new Cookie("birthtime", "568022401")
            {
                Domain = new Uri("http://store.steampowered.com/").Host
            };
            CookieContainer cc = new CookieContainer();
            cc.Add(cookie);
            request.CookieContainer = cc;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();
            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);


                string name = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[7]/div[4]/div[1]/div[2]/div[2]/div[2]/div/div[3]").InnerText;
                string price = doc.DocumentNode.SelectSingleNode("//*[@id=\"game_area_purchase\"]/div[1]/div/div[2]/div/div[1]").InnerText.Replace("\t", "");
                string info = doc.DocumentNode.SelectSingleNode("//*[@id=\"game_area_description\"]").InnerText.Replace("\t", "");

                return "Name: " + name + " price: " + price + " info: " + info;
            }

        }

        private static List<Game> SearchUplay(string term)
        {
            var games = new List<Game>();
            var html = @"https://store.ubi.com/us/search/?q=" + term + "&prefn1=productTypeCategoryRefinementString&prefv1=Video%20Game&lang=en_US#q=" + term + "&prefn1=productEditionString&prefv1=Standard&prefn2=productTypeCategoryRefinementString&prefv2=Video%20Game";
            var htmlDoc = web.Load(html);
            var results = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"search-result-items\"]");
            if (results == null) return games;
            foreach (var result in results.ChildNodes)
            {
                if (result.Name == "li")
                {
                    games.Add(new Game()
                    {
                        Name = result.Descendants("h2").First().InnerText.Replace("\n", "").Replace("\t", ""),
                        IsStoreItem = true,
                        PlayAction = new GameAction()
                        {
                            Type = GameActionType.URL,
                            Store = "Uplay",
                            Path = "https://store.ubi.com" + result.Descendants("div").Skip(1).First().Descendants("a").First().Attributes["data-link"].Value
                        }
                    });

                }
            }
            return games;
        }
        private static List<Game> SortListOnRelavance(List<Game> list, string search)
        {
            //Store name to relative count in dictionary
            Dictionary<string, List<int>> distinctGames = new Dictionary<string, List<int>>();

            for (int i=0; i<list.Count; i++)
            {
                Game game = list[i];
                // find the relevance value based on search string
                if (!distinctGames.ContainsKey(game.Name))
                {
                    distinctGames.Add(game.Name, new List<int>());
                }
                distinctGames[game.Name].Add(i);
            }
            List<Game> CombinedGames = new List<Game>();
            foreach (var distinctGame in distinctGames)
            {
                Game combinedGame = list[distinctGame.Value[0]];
                for (int i = 1; i < distinctGame.Value.Count; i++)
                {
                    int index = distinctGame.Value[i];
                    if (combinedGame.OtherActions == null) combinedGame.OtherActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>();
                    combinedGame.OtherActions.Add(list[index].PlayAction);
                }
                CombinedGames.Add(combinedGame);
            }
            return CombinedGames;
        }
    }
}
