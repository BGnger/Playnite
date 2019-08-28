using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Playnite.SDK.Models;

namespace Playnite.Common.Web
{
    public class WebScaper
    {
        public static List<Game> SearchForGame(string searchTerm)
        {
            var results = new List<Game>();
            results.AddRange(SearchSteam(searchTerm));
            results.AddRange(SearchUplay(searchTerm));
            return SortListOnRelavance(results, searchTerm);
        }

        private static HtmlWeb web = new HtmlWeb();
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
                        PlayAction = new GameAction()
                        {
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
            var newList = new List<Game>();
            foreach (var item in list)
            {
                // find the relevance value based on search string
                int count = Regex.Matches(Regex.Escape(item.Name.ToLower()), search.ToLower()).Count;
                newList.Insert(count, item);
            }

            Dictionary<string, Game> combinedResults = new Dictionary<string, Game>();
            foreach (var item in newList)
            {
                if (combinedResults.ContainsKey(item.Name))
                {
                    if(combinedResults[item.Name].OtherActions == null)
                    {
                        combinedResults[item.Name].OtherActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>();
                    }
                    combinedResults[item.Name].OtherActions.Add(new GameAction() {Store = item.PlayAction.Store, Path = item.PlayAction.Path });
                    
                }
                else
                {
                    combinedResults.Add(item.Name, item);
                }
            }
            var combinedList = new List<Game>();
            combinedResults.ToList().ForEach(r => combinedList.Add(r.Value));
            return combinedList;
        }
    }
}
