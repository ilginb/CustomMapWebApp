using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Project_IlginHolden.Models.DB;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.IO;

namespace Project_IlginHolden.Views
{
    public class DashboardController : Controller
    {
        //Adding role and user managers - Ilgin 11/09/2021
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        //And context
        private readonly FSQ3_Team1_ProjectContext _context;

        public DashboardController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, FSQ3_Team1_ProjectContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        //Adding authorization so non logged in users can't see the dashboard page -Ilgin 09.09.2021
        // This method returns a view that will create the default map to display to the user. - Holden 16/09/2021
        [Authorize]
        public async Task<IActionResult> Index()
        {
            string mapName = "Default";
            Map defaultMap = LoadMap(mapName);
            string mapGeometry = CreateMapObject(defaultMap.GeoJson);

            return View(model: mapGeometry);
        }

        //Adding authorization - Ilgin 09.09.2021
        //This method is called once a paypal payment has been made. Once the payment goes through, the user
        //is directed to the payment successful page. This method removes the users current roles and adds them to 
        //the role "PaidUser" in order to have full access to the app. - Ilgin 08.09.2021
        [Authorize]
        public async Task<IActionResult> PaymentSuccessfulAsync()
        {
            string user = this.User.FindFirst(ClaimTypes.Name).Value;
            var x = await _userManager.FindByNameAsync(user);
            var currentRoles = await _userManager.GetRolesAsync(x);
            await _userManager.RemoveFromRolesAsync(x, currentRoles);
            var result = await _userManager.AddToRoleAsync(x, "PaidUser");
            // var x = this.User.FindFirst(ClaimTypes.).Value;
            return View();
        }

        /* This method creates a list of all "Country" objects that are available in the database, which contain information about
         * the names and IDs of all countries that are able to be displayed on the website. A user will be able to select items
         * from this list later to create their custom maps. - Holden 16/09/2021
        */ 
        [Authorize(Roles="PaidUser")]
        public async Task<IActionResult> CreateMap()
        {
            List<Country> countryList = _context.Countries.Select(i => new Country() { Id = i.Id, Admin = i.Admin, Geometry = null, IsoA3 = null }).ToList();
            countryList.Sort((x, y) => string.Compare(x.Admin, y.Admin));
            return View(countryList);
        }

        /* This method loads a list of country IDs, uses the "CreateMapObject()" method to gather the geometric information of those
         * countries, and sends the user with this information to a page that draws a map from said information. - Holden 16/09/2021
        */
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisplayMap(IFormCollection countryForm)
        {
            string countryList = countryForm.ElementAt(0).Value;
            string mapGeometry = CreateMapObject(countryList);
            string[] countryData = new string[2];
            countryData[0] = countryList;
            countryData[1] = mapGeometry;

            return View(countryData);
        }

        /* This method takes a collection of inputs with information about a map to be created and saved to the database.
         * It also checks whether an identical map exists before attempting to add the map. - Holden 16/09/2021
         */
        [HttpPost]
        public async Task<IActionResult> SaveMap(IFormCollection saveForm)
        {
            string currentUser = User.Identity.Name;
            Map newMap = new Map();
            newMap.UserId = currentUser;
            StringValues nameValue = "";
            StringValues countryList = "";
            string responseMessage = "";
            if(!saveForm.TryGetValue("mapName", out nameValue))
            {
                responseMessage += "Map name from form does not exist.";
            }
            if(saveForm.TryGetValue("countryList", out countryList))
            {
                responseMessage += "Country list from form does not exist";
            }
            newMap.Name = nameValue.ToString();
            newMap.GeoJson = countryList.ToString();

            // The following section checks the database to see whether a map exists with the same details as those entered.
            // If a map with the same details exists, the new map will not be added to the database, and a response message
            // will be returned to the user. - Holden 16/09/2021
            bool mapExists = false;
            foreach (Map existing in _context.Maps)
            {
                if (existing.UserId == currentUser && existing.Name == newMap.Name)
                {
                    mapExists = true;
                    break;
                }
            }
            if (!mapExists)
            {
                _context.Maps.Add(newMap);
                _context.SaveChanges();
                responseMessage = "Map saved successfully!";
            }
            else
            {
                responseMessage = "You have already created a map with that name. Please try again.";
            }
            return RedirectToAction("ManageMaps", new { responseMessage });
        }

        // This method returns a view that contains a list of all maps associated with the user currently signed in. - Holden 16/09/2021
        public async Task<IActionResult> ManageMaps(string responseMessage)
        {
            string currentUser = User.Identity.Name;
            List<Map> mapList = _context.Maps.Where(u => u.UserId == currentUser).ToList();
            Map dummyMap = new Map();
            dummyMap.Name = "responseMessage";
            dummyMap.GeoJson = responseMessage;
            mapList.Insert(0, dummyMap);
            return View(mapList);
        }

        /* The following methods do not return information to the user. They are only available to the other methods within this controller.
         * - Holden 16/09/2021
         */

        /* This method is used by other methods within this controller to search for an existing map in the database
         * via the name of the map in the form of a string and the name of the User currently signed in. - Holden 16/09/2021
         */
        private Map LoadMap(string mapName)
        {
            Map fetchedMap;
            try
            {
                // Each map should have a unique name + user combination. - Holden 16/09/2021
                string currentUser = User.Identity.Name;
                fetchedMap = _context.Maps.Single(u => u.UserId == currentUser && u.Name == mapName);
            }
            catch
            {
                // If a map with those details can't be found, it will return the default world map instead. - Holden 16/09/2021
                fetchedMap = _context.Maps.First();
            }
            return fetchedMap;
        }

        /* This method takes a list of countries as a comma separated string and returns the geometric information of every country
         * in the list. - Holden 16/09/2021
         */
        private string CreateMapObject(string countryList)
        {
            string[] countries = countryList.Split(',');

            List<Country> countryResults = new List<Country>();
            foreach (string Id in countries)
            {
                countryResults.Add(_context.Countries.Find(Id));
            }
            // The hard-coded strings in this section are of the format required for the OpenLayers JavaScript library in the front-end
            // to function. - Holden 16/09/2021
            string createdMap = "{\"type\":\"FeatureCollection\",\"features\":[";
            for (int i = 0; i < countryResults.Count - 1; i++)
            {
                createdMap += "{\"type\":\"Feature\",\"properties\":{\"ADMIN\":\"" + countryResults[i].Admin + "\",\"ISO_A3\":\"" + countryResults[i].IsoA3 + "\"},\"geometry\":{" + countryResults[i].Geometry + "}},";
            }
            createdMap += "{\"type\":\"Feature\",\"properties\":{\"ADMIN\":\"" + countryResults.Last().Admin + "\",\"ISO_A3\":\"" + countryResults.Last().IsoA3 + "\"},\"geometry\":{" + countryResults.Last().Geometry + "}}]}";

            return createdMap;
        }

    }
}
