using LibraryData;
using LibraryData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibraryServices
{
    public class CheckoutService : ICheckout
    {
        private LibraryContext _context;

        public CheckoutService(LibraryContext context)
        {
            _context = context;
        }

        public void Add(Checkout newCheckout)
        {
            _context.Add(newCheckout);
            _context.SaveChanges();
        }

        public IEnumerable<Checkout> GetAll()
        {
            return _context.Checkouts;
        }

        public Checkout GetById(int checkoutId)
        {
            return GetAll()
                .FirstOrDefault(checkout => checkout.Id == checkoutId);
        }

        public IEnumerable<CheckoutHistory> GetCheckoutHistory(int id)
        {
            return _context.CheckoutHistories
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .Where(h => h.LibraryAsset.Id == id);
        }

        public IEnumerable<Hold> GetCurrentHolds(int id)
        {
            return _context.Holds
                .Include(h => h.LibraryAsset)
                .Where(h => h.LibraryAsset.Id == id);
        }

        public Checkout GetLatestCheckout(int assetId)
        {
            return _context.Checkouts
                .Where(c => c.LibraryAsset.Id == assetId)
                .OrderByDescending(c => c.Since)
                .FirstOrDefault();
        }

        public void MarkFound(int assetId)
        {
            this.UpdateAssetStatus(assetId, "Available");

            this.RemoveExistingCheckouts(assetId);

            this.CloseExistingCheckoutHistory(assetId);

            _context.SaveChanges();
        }

        public void MarkLost(int assetId)
        {
            this.UpdateAssetStatus(assetId, "Lost");
        }

        public void CheckInItem(int assetId, int libraryCardId)
        {
            var now = DateTime.Now;

            var item = _context.LibraryAssets.FirstOrDefault(a => a.Id == assetId);

            // Mark item for update
            _context.Update(item);

            // Remove any existing checkouts on the item
            this.RemoveExistingCheckouts(assetId);

            // Close any existing checkout history
            this.CloseExistingCheckoutHistory(assetId);

            // Look for existing holds on the item
            var currentHolds = _context.Holds
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .Where(h => h.LibraryAsset.Id == assetId);

            switch (currentHolds.Any())
            {
                case true:
                    // If there are holds, checkout the item to the library card with the earliest hold
                    this.CheckoutToEarliestHold(assetId, currentHolds);
                    goto default;
                default:
                    // Always update the item status to available
                    this.UpdateAssetStatus(assetId, "Available");
                    break;
            }

            _context.SaveChanges();
        }

        public void CheckOutItem(int assetId, int libraryCardId)
        {
            if (this.IsCheckedOut(assetId))
            {
                return;
            }
            else
            {
                var item = _context.LibraryAssets.FirstOrDefault(a => a.Id == assetId);

                this.UpdateAssetStatus(assetId, "Checked Out");

                var libraryCard = _context.LibraryCards
                    .Include(card => card.Checkouts)
                    .FirstOrDefault(card => card.Id == libraryCardId);

                var now = DateTime.Now;
                var checkout = new Checkout()
                {
                    LibraryAsset = item,
                    LibraryCard = libraryCard,
                    Since = now,
                    Until = this.GetDefaultCheckoutTime(now)
                };

                _context.Add(checkout);

                var checkoutHistory = new CheckoutHistory
                {
                    CheckedOut = now,
                    LibraryAsset = item,
                    LibraryCard = libraryCard
                };

                _context.Add(checkoutHistory);
                _context.SaveChanges();
            }
        }

        public void PlaceHold(int assetId, int libraryCardId)
        {
            var now = DateTime.Now;

            var asset = _context.LibraryAssets
                .FirstOrDefault(a => a.Id == assetId);

            var card = _context.LibraryCards
                .FirstOrDefault(c => c.Id == libraryCardId);

            if (asset.Status.Name == "Available")
            {
                UpdateAssetStatus(assetId, "Hold");
            }

            var hold = new Hold
            {
                HoldPlaced = now,
                LibraryAsset = asset,
                LibraryCard = card
            };

            _context.Add(hold);
            _context.SaveChanges();
        }

        public string GetCurrentHoldPatronName(int holdId)
        {
            var hold = _context.Holds
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .FirstOrDefault(h => h.Id == holdId);

            var cardId = hold?.LibraryCard.Id;

            var patron = _context.Patrons
                .Include(p => p.LibraryCard)
                .FirstOrDefault(p => p.LibraryCard.Id == cardId);

            return patron?.FirstName + " " + patron.LastName;
        }

        public DateTime GetCurrentHoldPlaced(int holdId)
        {
            return
                _context.Holds
                .Include(h => h.LibraryAsset)
                .Include(h => h.LibraryCard)
                .FirstOrDefault(h => h.Id == holdId)
                .HoldPlaced;
        }

        public Checkout GetCheckoutByAssetId(int assetId)
        {
            return
                _context.Checkouts
                .Include(cO => cO.LibraryAsset)
                .Include(cO => cO.LibraryCard)
                .FirstOrDefault(cO => cO.LibraryAsset.Id == assetId);
        }

        public string GetCurrentCheckoutPatron(int assetId)
        {
            var checkout = this.GetCheckoutByAssetId(assetId);
            if (checkout == null)
            {
                return "";
            }
            else
            {
                var cardId = checkout.LibraryCard.Id;

                var patron = _context.Patrons
                    .Include(p => p.LibraryCard)
                    .Where(p => p.LibraryCard.Id == cardId) as Patron;

                // If not first or default you have to cast

                return patron.FirstName + " " + patron.LastName;
            }
        }

        // Internal

        /// <summary>
        /// Check in an item entry by assigning a date to its checked in status.
        /// </summary>
        /// <param name="assetId">Id to search for.</param>
        private void CloseExistingCheckoutHistory(int assetId)
        {
            var history = _context.CheckoutHistories
                .FirstOrDefault(h => h.LibraryAsset.Id == assetId && h.CheckedIn == null);

            if (history != null)
            {
                var now = DateTime.Now;
                _context.Update(history);
                history.CheckedIn = now;
            }
        }

        /// <summary>
        /// Remove any existing checkouts for the item.
        /// </summary>
        /// <param name="assetId">Id to search for.</param>
        private void RemoveExistingCheckouts(int assetId)
        {
            var checkout = _context.Checkouts
                .FirstOrDefault(co => co.LibraryAsset.Id == assetId);

            if (checkout != null)
            {
                _context.Remove(checkout);
            }
        }

        /// <summary>
        /// Change an item's status.
        /// </summary>
        /// <param name="assetId">Id to search for.</param>
        /// <param name="newAssetStatus">Status to give it.</param>
        private void UpdateAssetStatus(int assetId, string newAssetStatus)
        {
            var item = _context.LibraryAssets
                .FirstOrDefault(a => a.Id == assetId);

            _context.Update(item);

            item.Status = _context.Statuses
                .FirstOrDefault(status => status.Name == newAssetStatus);
        }

        /// <summary>
        /// Checks an item out.
        /// </summary>
        /// <param name="assetId">Id to search for.</param>
        /// <param name="currentHolds">Hold list</param>
        private void CheckoutToEarliestHold(int assetId, IQueryable<Hold> currentHolds)
        {
            var earliestHold = currentHolds
                .OrderBy(holds => holds.HoldPlaced)
                .FirstOrDefault();

            var card = earliestHold.LibraryCard;

            _context.Remove(earliestHold);
            _context.SaveChanges();
            this.CheckOutItem(assetId, card.Id);
        }

        /// <summary>
        /// Checks if an asset is within the checkouts list.
        /// </summary>
        /// <param name="assetId">Asset to search for.</param>
        /// <returns>True or false indicating if the item has been checked out.</returns>
        private bool IsCheckedOut(int assetId)
        {
            var isCheckedOut = _context.Checkouts.Where(cO => cO.LibraryAsset.Id == assetId).Any();
            return isCheckedOut;
        }

        /// <summary>
        /// Generates a default checkout return due date.
        /// </summary>
        /// <param name="now">The time of the checkout.</param>
        /// <returns>The return due date based on the checkout time.</returns>
        private DateTime GetDefaultCheckoutTime(DateTime now)
        {
            return now.AddDays(30);
        }
    }
}
