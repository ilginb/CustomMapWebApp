using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project_IlginHolden.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Project_IlginHolden.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //Adding code to make sure if a user is signed in they go directly to dashboard. 
            //If not signed in they go to the main Index page - Ilgin 11.09.2021
            if (User.Identity.IsAuthenticated == true)
            {
                return Redirect("Dashboard/Index");
            }
            else
            {
                return View();

            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
