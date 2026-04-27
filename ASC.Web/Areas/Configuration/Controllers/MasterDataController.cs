using ASC.Business.Interfaces;
using ASC.Model;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.Configuration.Models;
using ASC.Web.Controllers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace ASC.Web.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : BaseController
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper)
        {
            _masterData = masterData;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();
            var masterKeysViewModel = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);
            // Hold all Master Keys in session
            HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);
            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel == null ? null : masterKeysViewModel.ToList(),
                MasterKeyInContext = new MasterDataKeyViewModel(),
                IsEdit = false
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        {
            masterKeys.MasterKeys = HttpContext.Session.GetSession<List<MasterDataKeyViewModel>>("MasterKeys");

            if (masterKeys.MasterKeyInContext == null)
                masterKeys.MasterKeyInContext = new MasterDataKeyViewModel();

            if (string.IsNullOrWhiteSpace(masterKeys.MasterKeyInContext.Name))
            {
                ModelState.AddModelError("MasterKeyInContext.Name", "Vui lòng nhập Name.");
                return View(masterKeys);
            }

            var masterKey = _mapper.Map<MasterDataKeyViewModel, MasterDataKey>(masterKeys.MasterKeyInContext);
            var currentUser = HttpContext.User.GetCurrentUserDetails().Name;

            if (masterKeys.IsEdit)
            {
                masterKey.UpdatedBy = currentUser;
                await _masterData.UpdateMasterKeyAsync(
                    masterKeys.MasterKeyInContext.PartitionKey,
                    masterKey
                );
            }
            else
            {
                masterKey.RowKey = Guid.NewGuid().ToString();
                masterKey.PartitionKey = masterKey.Name;
                masterKey.CreatedBy = currentUser;
                masterKey.UpdatedBy = currentUser;
                await _masterData.InsertMasterKeyAsync(masterKey);
            }

            return RedirectToAction("MasterKeys");
        }

        [HttpGet]
        public async Task<IActionResult> MasterValues(string selectedKey = null)
        {
            var keys = await _masterData.GetAllMasterKeysAsync();

            ViewBag.MasterKeys = keys
                .GroupBy(x => x.PartitionKey)
                .Select(g => g.First())
                .ToList();

            ViewBag.SelectedKey = selectedKey;

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            // Get Master values based on master key.
            return Json(new { data = await _masterData.GetAllMasterValuesByKeyAsync(key) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                return Json("Error");
            }

            var masterDataValue = _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

            if (isEdit)
            {
                await _masterData.UpdateMasterValueAsync(
                    masterDataValue.PartitionKey,
                    masterDataValue.RowKey,
                    masterDataValue);
            }
            else
            {
                masterDataValue.RowKey = Guid.NewGuid().ToString();
                masterDataValue.CreatedBy = HttpContext.User.GetCurrentUserDetails().Name;
                masterDataValue.UpdatedBy = HttpContext.User.GetCurrentUserDetails().Name;

                // Lấy MasterDataKey theo PartitionKey và gán navigation property
                var masterKeys = await _masterData.GetMaserKeyByNameAsync(masterDataValue.PartitionKey);
                var key = masterKeys.FirstOrDefault();
                if (key != null)
                {
                    masterDataValue.MasterDataKey = key; // ← gán navigation property
                }

                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            return Json(new { Success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            try
            {
                var files = Request.Form.Files;

                if (!files.Any())
                    return Json(new { success = false, message = "No file" });

                var excelFile = files.First();

                var masterData = await ParseMasterDataExcel(excelFile);

                var result = await _masterData.UploadBulkMasterData(masterData);

                return Json(new
                {
                    Success = result,
                    Key = masterData.FirstOrDefault()?.PartitionKey,
                    Text = result ? "Upload success" : "Upload failed"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Admin");

            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using (var package = new ExcelPackage(memoryStream))
                {
                    // CHECK SHEET
                    if (package.Workbook.Worksheets.Count == 0)
                        return masterValueList;

                    var worksheet = package.Workbook.Worksheets[0];

                    // CHECK DIMENSION
                    if (worksheet.Dimension == null)
                        return masterValueList;

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var partitionKey = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            var name = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                            var isActiveStr = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                            // BỎ DÒNG RÁC
                            if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(name))
                                continue;

                            bool isActive = false;
                            bool.TryParse(isActiveStr, out isActive);

                            masterValueList.Add(new MasterDataValue
                            {
                                RowKey = Guid.NewGuid().ToString(),
                                PartitionKey = partitionKey,
                                Name = name,
                                IsActive = isActive,
                                CreatedBy = "Admin",
                                UpdatedBy = "Admin"
                            });
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            return masterValueList;
        }
    }
}