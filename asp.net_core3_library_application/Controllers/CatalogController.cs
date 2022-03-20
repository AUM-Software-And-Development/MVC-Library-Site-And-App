using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryData;
using LibraryServices;
using asp.net_core3_library_application.Models.Catalog;
using static asp.net_core3_library_application.Models.Catalog.AssetDetailModel;
using asp.net_core3_library_application.Models.CheckoutModels;

namespace asp.net_core3_library_application.Controllers
{
    public class CatalogController : Controller
    {
        private ILibraryAsset _assets;
        private ICheckout _checkouts;

        // Params are registered as a service in startup.cs
        public CatalogController(ILibraryAsset assets, ICheckout checkouts)
        {
            _assets = assets;
            _checkouts = checkouts;
        }

        public IActionResult Index()
        {
            var assetModels = _assets.GetAll();

            // How to map a result to a class
            var listingResult = assetModels
                .Select(result => new AssetIndexListingModel
                {
                    Id = result.Id,
                    ImageUrl = result.ImageUrl,
                    AuthorOrDirector = _assets.GetGetAuthorOrDirector(result.Id),
                    DeweyCallNumber = _assets.GetDeweyIndex(result.Id),
                    Title = result.Title,
                    Type = _assets.GetType(result.Id)
                });

            // Model data storage aka render values/context
            var model = new AssetIndexModel()
            {
                Assets = listingResult
            };

            return View(model);
        }

        public IActionResult Detail(int id)
        {
            //var asset = _assets.GetById(id);

            //var model = new AssetDetailModel
            //{
            //    AssetId = id,
            //    Title = asset.Title,
            //    Year = asset.Year,
            //    Cost = asset.Cost,
            //    Status = asset.Status.Name,
            //    ImageUrl = asset.ImageUrl,
            //    AuthorOrDirector = _assets.GetGetAuthorOrDirector(id),
            //    CurrentLocation = _assets.GetCurrentLocation(id).Name,
            //    DeweyCallNumber = _assets.GetDeweyIndex(id),
            //    ISBN = _assets.GetIsbn(id)
            //};

            var asset = _assets.GetById(id);
            var currentHolds = _checkouts.GetCurrentHolds(id)
                .Select(a => new AssetHoldModel {
                    HoldPlaced = _checkouts.GetCurrentHoldPlaced(a.Id).ToString("d"),
                    PatronName = _checkouts.GetCurrentHoldPatronName(a.Id)
                });;

            var model = new AssetDetailModel
            {
                AssetId = id,
                Title = asset.Title,
                Type = _assets.GetType(id),
                Year = asset.Year,
                Cost = asset.Cost,
                Status = asset.Status.Name,
                ImageUrl = asset.ImageUrl,
                AuthorOrDirector = _assets.GetGetAuthorOrDirector(id),
                CurrentLocation = _assets.GetCurrentLocation(id).Name,
                DeweyCallNumber = _assets.GetDeweyIndex(id),
                CheckoutHistory = _checkouts.GetCheckoutHistory(id),
                ISBN = _assets.GetIsbn(id),
                PatronName = _checkouts.GetCurrentCheckoutPatron(id),
                CurrentHolds = currentHolds
            };

            return View(model);
        }

        public IActionResult Checkout(int id)
        {
            var asset = _assets.GetById(id);

            var model = new CheckoutModel
            {
                AssetId = id,
                ImageUrl = asset.ImageUrl,
                Title = asset.Title,
                LibraryCardId = "",
                IsCheckedOut = _checkouts.IsCheckedOut(id)
            };

            return View(model);
        }

        public IActionResult CheckIn(int id)
        {
            _checkouts.CheckInItem(id);
            return RedirectToAction("Detail", new { id = id });

        }

        public IActionResult Hold(int id)
        {
            var asset = _assets.GetById(id);

            var model = new CheckoutModel
            {
                AssetId = id,
                ImageUrl = asset.ImageUrl,
                Title = asset.Title,
                LibraryCardId = "",
                IsCheckedOut = _checkouts.IsCheckedOut(id),
                HoldCount = _checkouts.GetCurrentHolds(id).Count()
            };

            return View(model);
    }

        public IActionResult MarkLost(int id)
        {
            _checkouts.MarkLost(id);

            return RedirectToAction("Detail", new { id = id });
        }

        [HttpPost] // Limit to post
        public IActionResult PlaceCheckout(int AssetId, int libraryCardId)
        {
            _checkouts.CheckOutItem(AssetId, libraryCardId);

            return RedirectToAction("Detail", new { id = AssetId });
        }

        [HttpPost]
        public IActionResult PlaceHold(int AssetId, int libraryCardId)
        {
            _checkouts.PlaceHold(AssetId, libraryCardId);

            return RedirectToAction("Detail", new { id = AssetId });
        }
    }
}
