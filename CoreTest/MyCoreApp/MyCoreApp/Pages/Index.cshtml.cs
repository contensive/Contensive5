using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCoreApp.Pages {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger) {
            _logger = logger;

            using (var cp = new Contensive.Processor.CPClass("app210530")) {
                cp.CdnFiles.Append("coreAppTest.log", DateTime.Now.ToString() + "-index, constructor\r\n");
            }
        }

        public void OnGet() {
            using (var cp = new Contensive.Processor.CPClass("app210530")) {
                cp.CdnFiles.Append("coreAppTest.log", DateTime.Now.ToString() + "-index, onget\r\n");
            }

        }
    }
}
