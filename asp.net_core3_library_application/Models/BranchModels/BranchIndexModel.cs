using System.Collections.Generic;

namespace asp.net_core3_library_application.Models.BranchModels
{
    public class BranchIndexModel
    {
        public IEnumerable<BranchDetailModel> Branches { get; set; }
    }
}
