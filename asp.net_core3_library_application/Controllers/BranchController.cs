using asp.net_core3_library_application.Models.BranchModels;
using LibraryData;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace asp.net_core3_library_application.Controllers
{
    public class BranchController : Controller
    {
        private ILibraryBranch _branch;
        public BranchController(ILibraryBranch branch)
        {
            _branch = branch;
        }

        public IActionResult Index()
        {
            var branches = _branch.GetAll().Select(branch => new BranchDetailModel
            {
                Id = branch.Id,
                Name = branch.Name,
                IsOpen = _branch.IsBranchOpen(branch.Id),
                NumberOfAssets = _branch.GetAssets(branch.Id).Count(),
                NumberOfPatrons = _branch.GetPatrons(branch.Id).Count()
            });

            var model = new BranchIndexModel()
            {
                Branches = branches
            };

            return View();
        }
    }
}
