using ASC.Business.Interfaces;
using ASC.Model;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.Configuration.Models;
using ASC.Web.Controllers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        private static bool _isEpplusLicenseSet = false;
        private static readonly object _epplusLicenseLock = new object();

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper)
        {
            _masterData = masterData;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();

            var masterKeysViewModel =
                _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);

            HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);

            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel?.ToList() ?? new List<MasterDataKeyViewModel>(),
                MasterKeyInContext = new MasterDataKeyViewModel(),
                IsEdit = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        {
            masterKeys.MasterKeys =
                HttpContext.Session.GetSession<List<MasterDataKeyViewModel>>("MasterKeys")
                ?? new List<MasterDataKeyViewModel>();

            if (masterKeys.MasterKeyInContext == null)
            {
                masterKeys.MasterKeyInContext = new MasterDataKeyViewModel();
            }

            if (string.IsNullOrWhiteSpace(masterKeys.MasterKeyInContext.Name))
            {
                ModelState.AddModelError("MasterKeyInContext.Name", "Vui lòng nhập Name.");
                return View(masterKeys);
            }

            var currentUser = GetCurrentUserName();

            if (!masterKeys.IsEdit)
            {
                masterKeys.MasterKeyInContext.RowKey = Guid.NewGuid().ToString();
                masterKeys.MasterKeyInContext.PartitionKey = masterKeys.MasterKeyInContext.Name.Trim();
                masterKeys.MasterKeyInContext.CreatedBy = currentUser;
                masterKeys.MasterKeyInContext.UpdatedBy = currentUser;
            }
            else
            {
                masterKeys.MasterKeyInContext.UpdatedBy = currentUser;
            }

            var masterKey =
                _mapper.Map<MasterDataKeyViewModel, MasterDataKey>(masterKeys.MasterKeyInContext);

            if (masterKeys.IsEdit)
            {
                await _masterData.UpdateMasterKeyAsync(
                    masterKeys.MasterKeyInContext.PartitionKey,
                    masterKey
                );
            }
            else
            {
                await _masterData.InsertMasterKeyAsync(masterKey);
            }

            return RedirectToAction("MasterKeys");
        }

        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();

            ViewBag.MasterKeys = masterKeys
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.PartitionKey)
                .ToList();

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                MasterValueInContext = new MasterDataValueViewModel(),
                IsEdit = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Json(new { data = new List<MasterDataValue>() });
            }

            var masterValues = await _masterData.GetAllMasterValuesByKeyAsync(key.Trim());

            return Json(new { data = masterValues });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            try
            {
                if (masterValue == null)
                {
                    return BadRequest(new
                    {
                        Error = true,
                        Text = "Không nhận được dữ liệu Master Value."
                    });
                }

                if (string.IsNullOrWhiteSpace(masterValue.PartitionKey))
                {
                    return BadRequest(new
                    {
                        Error = true,
                        Text = "Vui lòng chọn Partition Key."
                    });
                }

                if (string.IsNullOrWhiteSpace(masterValue.Name))
                {
                    return BadRequest(new
                    {
                        Error = true,
                        Text = "Vui lòng nhập Name."
                    });
                }

                var currentUser = GetCurrentUserName();

                var masterDataValue =
                    _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

                masterDataValue.PartitionKey = masterValue.PartitionKey.Trim();
                masterDataValue.Name = masterValue.Name.Trim();

                var masterKey = await GetMasterKeyByPartitionKeyAsync(masterDataValue.PartitionKey);

                if (masterKey == null)
                {
                    return BadRequest(new
                    {
                        Error = true,
                        Text = $"Partition Key '{masterDataValue.PartitionKey}' chưa tồn tại trong Master Keys."
                    });
                }

                PrepareMasterDataValueForSave(
                    masterDataValue,
                    masterKey,
                    currentUser,
                    isEdit
                );

                if (isEdit)
                {
                    if (string.IsNullOrWhiteSpace(masterDataValue.RowKey))
                    {
                        return BadRequest(new
                        {
                            Error = true,
                            Text = "Không tìm thấy RowKey để cập nhật."
                        });
                    }

                    await _masterData.UpdateMasterValueAsync(
                        masterDataValue.PartitionKey,
                        masterDataValue.RowKey,
                        masterDataValue
                    );
                }
                else
                {
                    masterDataValue.RowKey = Guid.NewGuid().ToString();

                    await _masterData.InsertMasterValueAsync(masterDataValue);
                }

                return Json(new
                {
                    Success = true,
                    Text = isEdit ? "Cập nhật thành công." : "Tạo mới thành công."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = true,
                    Text = GetFullExceptionMessage(ex),
                    Detail = ex.InnerException?.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            try
            {
                var files = Request.Form.Files;

                if (files == null || !files.Any())
                {
                    return BadRequest(new
                    {
                        Error = true,
                        Text = "Vui lòng chọn file Excel."
                    });
                }

                var excelFile = files.First();

                ValidateExcelFile(excelFile);

                var masterData = await ParseMasterDataExcel(excelFile);

                if (masterData.Count == 0)
                {
                    return BadRequest(new
                    {
                        Error = true,
                        Text = "File Excel không có dòng dữ liệu nào để upload."
                    });
                }

                var currentUser = GetCurrentUserName();

                await EnsureMasterKeysExistForUploadAsync(masterData, currentUser);
                await PrepareMasterDataValuesForBulkSaveAsync(masterData, currentUser);

                var result = await _masterData.UploadBulkMasterData(masterData);

                return Json(new
                {
                    Success = result,
                    Count = masterData.Count,
                    Text = result
                        ? $"Upload thành công {masterData.Count} dòng dữ liệu."
                        : "Upload thất bại."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Error = true,
                    Text = ex.Message,
                    Detail = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = true,
                    Text = GetFullExceptionMessage(ex),
                    Detail = ex.InnerException?.Message
                });
            }
        }

        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            EnsureEpplusLicense();

            var masterValueList = new List<MasterDataValue>();
            var currentUser = GetCurrentUserName();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        throw new InvalidOperationException("File Excel không có sheet hoặc không có dữ liệu.");
                    }

                    ValidateExcelHeaders(worksheet);

                    int rowCount = worksheet.Dimension.Rows;

                    if (rowCount < 2)
                    {
                        throw new InvalidOperationException("File Excel chỉ có header, chưa có dữ liệu.");
                    }

                    var duplicateChecker = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var partitionKey = worksheet.Cells[row, 1].Text?.Trim();
                        var name = worksheet.Cells[row, 2].Text?.Trim();
                        var isActiveText = worksheet.Cells[row, 3].Text?.Trim();

                        bool isEmptyRow =
                            string.IsNullOrWhiteSpace(partitionKey) &&
                            string.IsNullOrWhiteSpace(name) &&
                            string.IsNullOrWhiteSpace(isActiveText);

                        if (isEmptyRow)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(partitionKey))
                        {
                            throw new InvalidOperationException($"Dòng {row}: cột MasterKey bị trống.");
                        }

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            throw new InvalidOperationException($"Dòng {row}: cột MasterValue bị trống.");
                        }

                        if (string.IsNullOrWhiteSpace(isActiveText))
                        {
                            throw new InvalidOperationException($"Dòng {row}: cột IsActive bị trống.");
                        }

                        if (!TryParseIsActive(isActiveText, out bool isActive))
                        {
                            throw new InvalidOperationException(
                                $"Dòng {row}: IsActive phải là TRUE/FALSE, 1/0, YES/NO hoặc ACTIVE/INACTIVE."
                            );
                        }

                        var duplicateKey = $"{partitionKey}|{name}";

                        if (!duplicateChecker.Add(duplicateKey))
                        {
                            throw new InvalidOperationException(
                                $"Dòng {row}: dữ liệu bị trùng trong file Excel: {partitionKey} - {name}."
                            );
                        }

                        var masterDataValue = new MasterDataValue
                        {
                            RowKey = Guid.NewGuid().ToString(),
                            PartitionKey = partitionKey,
                            Name = name,
                            Value = name,
                            IsActive = isActive,
                            CreatedBy = currentUser,
                            UpdatedBy = currentUser
                        };

                        TrySetProperty(masterDataValue, "IsDeleted", false);
                        TrySetProperty(masterDataValue, "CreatedDate", DateTime.UtcNow);
                        TrySetProperty(masterDataValue, "UpdatedDate", DateTime.UtcNow);

                        masterValueList.Add(masterDataValue);
                    }
                }
            }

            return masterValueList;
        }

        private async Task EnsureMasterKeysExistForUploadAsync(
            List<MasterDataValue> masterData,
            string currentUser)
        {
            var masterKeyDictionary = await GetMasterKeyDictionaryAsync();

            var excelKeys = masterData
                .Select(x => x.PartitionKey?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var key in excelKeys)
            {
                if (key == null)
                {
                    continue;
                }

                if (masterKeyDictionary.ContainsKey(key))
                {
                    continue;
                }

                var newMasterKey = new MasterDataKey
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = key,
                    Name = key,
                    IsActive = true,
                    CreatedBy = currentUser,
                    UpdatedBy = currentUser
                };

                TrySetProperty(newMasterKey, "IsDeleted", false);
                TrySetProperty(newMasterKey, "CreatedDate", DateTime.UtcNow);
                TrySetProperty(newMasterKey, "UpdatedDate", DateTime.UtcNow);

                await _masterData.InsertMasterKeyAsync(newMasterKey);
            }
        }

        private async Task PrepareMasterDataValuesForBulkSaveAsync(
            List<MasterDataValue> masterData,
            string currentUser)
        {
            var masterKeyDictionary = await GetMasterKeyDictionaryAsync();

            foreach (var item in masterData)
            {
                if (string.IsNullOrWhiteSpace(item.PartitionKey))
                {
                    throw new InvalidOperationException("Có dòng dữ liệu thiếu PartitionKey.");
                }

                var partitionKey = item.PartitionKey.Trim();

                if (!masterKeyDictionary.TryGetValue(partitionKey, out var masterKey))
                {
                    throw new InvalidOperationException(
                        $"Partition Key '{partitionKey}' chưa tồn tại trong Master Keys."
                    );
                }

                PrepareMasterDataValueForSave(
                    item,
                    masterKey,
                    currentUser,
                    isEdit: false
                );
            }
        }

        private void PrepareMasterDataValueForSave(
            MasterDataValue masterDataValue,
            MasterDataKey masterKey,
            string currentUser,
            bool isEdit)
        {
            masterDataValue.PartitionKey = masterDataValue.PartitionKey.Trim();
            masterDataValue.Name = masterDataValue.Name.Trim();

            if (string.IsNullOrWhiteSpace(masterDataValue.Value))
            {
                masterDataValue.Value = masterDataValue.Name;
            }
            else
            {
                masterDataValue.Value = masterDataValue.Value.Trim();
            }

            masterDataValue.UpdatedBy = currentUser;

            if (!isEdit)
            {
                if (string.IsNullOrWhiteSpace(masterDataValue.CreatedBy))
                {
                    masterDataValue.CreatedBy = currentUser;
                }

                TrySetProperty(masterDataValue, "CreatedDate", DateTime.UtcNow);
            }

            TrySetProperty(masterDataValue, "UpdatedDate", DateTime.UtcNow);
            TrySetProperty(masterDataValue, "IsDeleted", false);

            TrySetProperty(masterDataValue, "MasterDataKeyPartitionKey", masterKey.PartitionKey);
            TrySetProperty(masterDataValue, "MasterDataKeyRowKey", masterKey.RowKey);

            var masterKeyId =
                GetPropertyValue(masterKey, "Id")
                ?? GetPropertyValue(masterKey, "MasterDataKeyId");

            if (masterKeyId != null)
            {
                TrySetProperty(masterDataValue, "MasterDataKeyId", masterKeyId);
            }
        }

        private async Task<MasterDataKey?> GetMasterKeyByPartitionKeyAsync(string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                return null;
            }

            var masterKeys = await _masterData.GetAllMasterKeysAsync();

            return masterKeys.FirstOrDefault(x =>
                string.Equals(
                    x.PartitionKey?.Trim(),
                    partitionKey.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }

        private async Task<Dictionary<string, MasterDataKey>> GetMasterKeyDictionaryAsync()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();

            return masterKeys
                .Where(x => !string.IsNullOrWhiteSpace(x.PartitionKey))
                .GroupBy(x => x.PartitionKey.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase
                );
        }

        private static void ValidateExcelFile(IFormFile excelFile)
        {
            if (excelFile == null)
            {
                throw new InvalidOperationException("Không nhận được file Excel.");
            }

            if (excelFile.Length <= 0)
            {
                throw new InvalidOperationException("File Excel đang rỗng.");
            }

            var extension = Path.GetExtension(excelFile.FileName);

            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ file Excel định dạng .xlsx.");
            }
        }

        private static void ValidateExcelHeaders(ExcelWorksheet worksheet)
        {
            var column1 = NormalizeHeader(worksheet.Cells[1, 1].Text);
            var column2 = NormalizeHeader(worksheet.Cells[1, 2].Text);
            var column3 = NormalizeHeader(worksheet.Cells[1, 3].Text);

            bool isColumn1Valid =
                column1 == "masterkey" ||
                column1 == "partitionkey";

            bool isColumn2Valid =
                column2 == "mastervalue" ||
                column2 == "name";

            bool isColumn3Valid =
                column3 == "isactive";

            if (!isColumn1Valid || !isColumn2Valid || !isColumn3Valid)
            {
                throw new InvalidOperationException(
                    "File Excel sai mẫu. Dòng đầu tiên phải có 3 cột: MasterKey, MasterValue, IsActive."
                );
            }
        }

        private static string NormalizeHeader(string? text)
        {
            return (text ?? string.Empty)
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .ToLowerInvariant();
        }

        private static bool TryParseIsActive(string? value, out bool result)
        {
            result = false;

            var text = (value ?? string.Empty).Trim().ToLowerInvariant();

            switch (text)
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                case "active":
                case "enable":
                case "enabled":
                case "có":
                case "co":
                    result = true;
                    return true;

                case "false":
                case "0":
                case "no":
                case "n":
                case "inactive":
                case "disable":
                case "disabled":
                case "không":
                case "khong":
                    result = false;
                    return true;

                default:
                    return false;
            }
        }

        private string GetCurrentUserName()
        {
            var userDetails = HttpContext.User.GetCurrentUserDetails();

            if (!string.IsNullOrWhiteSpace(userDetails?.Name))
            {
                return userDetails.Name;
            }

            if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
            {
                return User.Identity.Name;
            }

            return "System";
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var currentException = ex;

            while (currentException != null)
            {
                if (!string.IsNullOrWhiteSpace(currentException.Message))
                {
                    messages.Add(currentException.Message);
                }

                currentException = currentException.InnerException;
            }

            return string.Join(" | ", messages.Distinct());
        }

        private static object? GetPropertyValue(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName);

            if (property == null)
            {
                return null;
            }

            return property.GetValue(target);
        }

        private static void TrySetProperty(object target, string propertyName, object? value)
        {
            var property = target.GetType().GetProperty(propertyName);

            if (property == null || !property.CanWrite)
            {
                return;
            }

            if (value == null)
            {
                var isNullable =
                    !property.PropertyType.IsValueType ||
                    Nullable.GetUnderlyingType(property.PropertyType) != null;

                if (isNullable)
                {
                    property.SetValue(target, null);
                }

                return;
            }

            var targetType =
                Nullable.GetUnderlyingType(property.PropertyType)
                ?? property.PropertyType;

            object convertedValue;

            if (targetType == typeof(Guid))
            {
                convertedValue = value is Guid guidValue
                    ? guidValue
                    : Guid.Parse(value.ToString()!);
            }
            else if (targetType.IsEnum)
            {
                convertedValue = Enum.Parse(targetType, value.ToString()!);
            }
            else
            {
                convertedValue = Convert.ChangeType(value, targetType);
            }

            property.SetValue(target, convertedValue);
        }

        private static void EnsureEpplusLicense()
        {
            if (_isEpplusLicenseSet)
            {
                return;
            }

            lock (_epplusLicenseLock)
            {
                if (_isEpplusLicenseSet)
                {
                    return;
                }

                ExcelPackage.License.SetNonCommercialPersonal("BiChienBinh");

                _isEpplusLicenseSet = true;
            }
        }
    }
}