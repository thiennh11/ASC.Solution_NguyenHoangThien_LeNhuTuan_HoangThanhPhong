using ASC.Business.Interfaces;
using ASC.Model.BaseTypes;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.ServiceRequests.Models;
using ASC.Web.Controllers;
using ASC.Web.Data;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ASC.Web.Areas.ServiceRequests.Controllers
{
    [Area("ServiceRequests")]
    public class ServiceRequestController : BaseController
    {
        private readonly IServiceRequestOperations _serviceRequestOperations;
        private readonly IMapper _mapper;
        private readonly IMasterDataCacheOperations _masterData;

        public ServiceRequestController(IServiceRequestOperations operations,
            IMapper mapper,
            IMasterDataCacheOperations masterData)
        {
            _serviceRequestOperations = operations;
            _mapper = mapper;
            _masterData = masterData;
        }

        [HttpGet]
        public async Task<IActionResult> ServiceRequest()
        {
            var masterData = await _masterData.GetMasterDataCacheAsync();
            ViewBag.VehicleTypes = masterData.Values
                .Where(p => p.PartitionKey == MasterKeys.VehicleType.ToString()).ToList();
            ViewBag.VehicleNames = masterData.Values
                .Where(p => p.PartitionKey == MasterKeys.VehicleName.ToString()).ToList();
            return View(new NewServiceRequestViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ServiceRequest(NewServiceRequestViewModel request)
        {
            if (!ModelState.IsValid)
            {
                var masterData = await _masterData.GetMasterDataCacheAsync();
                ViewBag.VehicleTypes = masterData.Values
                    .Where(p => p.PartitionKey == MasterKeys.VehicleType.ToString()).ToList();
                ViewBag.VehicleNames = masterData.Values
                    .Where(p => p.PartitionKey == MasterKeys.VehicleName.ToString()).ToList();
                return View(request);
            }

            var serviceRequest = _mapper.Map<NewServiceRequestViewModel, ServiceRequest>(request);
            serviceRequest.PartitionKey = HttpContext.User.GetCurrentUserDetails().Email;
            serviceRequest.RowKey = Guid.NewGuid().ToString();
            serviceRequest.RequestedDate = request.RequestedDate;
            serviceRequest.Status = Status.New.ToString();
            serviceRequest.CreatedBy = HttpContext.User.GetCurrentUserDetails().Email;
            serviceRequest.UpdatedBy = HttpContext.User.GetCurrentUserDetails().Email;
            serviceRequest.ServiceEngineer = " ";
            await _serviceRequestOperations.CreateServiceRequestAsync(serviceRequest);
            return RedirectToAction("Dashboard", "Dashboard", new { Area = "ServiceRequests" });
        }
        public class AdminController : Controller
        {
            private readonly UserManager<IdentityUser> _userManager;

            public AdminController(UserManager<IdentityUser> userManager)
            {
                _userManager = userManager;
            }

            public async Task<IActionResult> DeleteEngineersExceptOne()
            {
                var users = _userManager.Users.ToList();

                foreach (var user in users)
                {
                    if (await _userManager.IsInRoleAsync(user, "Engineer"))
                    {
                        if (user.Email != "huutvtdmu@gmail.com")
                        {
                            await _userManager.DeleteAsync(user);
                        }
                    }
                }

                return Content("Deleted engineers");
            }
        }
    }
}