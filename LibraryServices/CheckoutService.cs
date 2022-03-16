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
            UpdateAssetStatus(assetId, "Available");

            RemoveExistingCheckouts(assetId);

            CloseExistingCheckoutHistory(assetId);

            _context.SaveChanges();
        }

        public void MarkLost(int assetId)
        {
            UpdateAssetStatus(assetId, "Lost");
        }

        public void CheckInItem(int assetId, int libraryCardId)
        {
            var now = DateTime.Now;

            var item = _context.LibraryAssets.FirstOrDefault(a => a.Id == assetId);

            // Mark item for update
            _context.Update(item);

            // Remove any existing checkouts on the item
            // Close any existing checkout history
            // Look for existing holds on the item
            // If there are holds, checkout the item to the library card with the earliest hold
            // else update the item status to available
        }

        public void PlaceHold(int assetId, int libraryCardId)
        {
            throw new NotImplementedException();
        }

        public void CheckOutItem(int assetId, int libraryCardId)
        {
            throw new NotImplementedException();
        }

        public string GetCurrentHoldPatronName(int id)
        {
            throw new NotImplementedException();
        }

        public DateTime GetCurrentHoldPlaced(int id)
        {
            throw new NotImplementedException();
        }

        // Internal

        /// <summary>
        /// Check in an item entry by assigning a date to its checked in status.
        /// </summary>
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
        private void UpdateAssetStatus(int assetId, string newAssetStatus)
        {
            var item = _context.LibraryAssets
                .FirstOrDefault(a => a.Id == assetId);

            _context.Update(item);

            item.Status = _context.Statuses
                .FirstOrDefault(status => status.Name == newAssetStatus);
        }
    }
}
