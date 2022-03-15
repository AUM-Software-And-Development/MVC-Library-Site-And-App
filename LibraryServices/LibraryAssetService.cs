using LibraryData;
using LibraryData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryServices
{
    public class LibraryAssetService : ILibraryAsset
    {
        private LibraryContext _context;

        public LibraryAssetService(LibraryContext context)
        {
            this._context = context;
        }

        public void Add(LibraryAsset newAsset)
        {
            this._context.Add(newAsset);
            this._context.SaveChanges();
        }

        public IEnumerable<LibraryAsset> GetAll()
        {
            return this._context.LibraryAssets.Include(asset => asset.Status).Include(asset => asset.Location);
        }

        public LibraryAsset GetById(int id)
        {
            return GetAll()
                // Allows null without throwing an error
                .FirstOrDefault(asset => asset.Id == id);
        }

        public LibraryBranch GetCurrentLocation(int id)
        {
            return GetById(id).Location;
                // this._context.LibraryAssets.FirstOrDefault(asset => asset.Id == id).Location;
        }

        public string GetDeweyIndex(int id)
        {
            // Discriminator
            if (this._context.Books.Any(book => book.Id == id))
            {
                return this._context.Books.FirstOrDefault(book => book.Id == id).DeweyIndex;
            }
            else return string.Empty;
        }

        public string GetIsbn(int id)
        {
            if (this._context.Books.Any(book => book.Id == id))
            {
                return this._context.Books.FirstOrDefault(book => book.Id == id).ISBN;
            }
            else return string.Empty;
        }

        public string GetTitle(int id)
        {
            return this._context.LibraryAssets.FirstOrDefault(a => a.Id == id).Title;
        }

        public string GetType(int id)
        {
            var book = this._context.LibraryAssets.OfType<Book>().Where(book => book.Id == id);
            return book.Any() ? "Book" : "Video";
        }

        public string GetGetAuthorOrDirector(int id)
        {
            var isBook = this._context.LibraryAssets.OfType<Book>()
                .Where(asset => asset.Id == id).Any();
            var isVideo = this._context.LibraryAssets.OfType<Video>()
                .Where(asset => asset.Id == id).Any();

            return isBook ? 
                this._context.Books.FirstOrDefault(book => book.Id == id).Author :
                this._context.Videos.FirstOrDefault(video => video.Id == id).Director
                ?? "Unknown";
        }
    }
}
