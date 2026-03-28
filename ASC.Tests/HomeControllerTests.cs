using ASC.Tests.TestUtilities;
using ASC.Utilities;
using ASC.Web.Configuration;
using ASC.Web.Controllers;
using ASC.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ASC.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<IOptions<ApplicationSettings>> optionsMock;
        private readonly Mock<HttpContext> mockHttpContext;
        private readonly Mock<ILogger<HomeController>> loggerMock;
        private readonly Mock<IEmailSender> emailSenderMock;

        public HomeControllerTests()
        {
            optionsMock = new Mock<IOptions<ApplicationSettings>>();

            mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(p => p.Session).Returns(new FakeSession());

            loggerMock = new Mock<ILogger<HomeController>>();
            emailSenderMock = new Mock<IEmailSender>();

            optionsMock.Setup(ap => ap.Value).Returns(new ApplicationSettings
            {
                ApplicationTitle = "ASC"
            });
        }

        private HomeController CreateController()
        {
            var controller = new HomeController(loggerMock.Object, optionsMock.Object);

            var services = new ServiceCollection();
            services.AddSingleton(emailSenderMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            mockHttpContext.Setup(p => p.RequestServices).Returns(serviceProvider);

            controller.ControllerContext.HttpContext = mockHttpContext.Object;
            controller.TempData = new TempDataDictionary(mockHttpContext.Object, Mock.Of<ITempDataProvider>());
            controller.ViewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary());

            return controller;
        }

        [Fact]
        public void HomeController_Index_View_Test()
        {
            var controller = CreateController();

            Assert.IsType<ViewResult>(controller.Index(emailSenderMock.Object));
        }

        [Fact]
        public void HomeController_Index_NoModel_Test()
        {
            var controller = CreateController();

            Assert.Null((controller.Index(emailSenderMock.Object) as ViewResult).ViewData.Model);
        }

        [Fact]
        public void HomeController_Index_Validation_Test()
        {
            var controller = CreateController();

            Assert.Equal(0, (controller.Index(emailSenderMock.Object) as ViewResult).ViewData.ModelState.ErrorCount);
        }

        [Fact]
        public void HomeController_Index_Session_Test()
        {
            var controller = CreateController();

            controller.Index(emailSenderMock.Object);

            // Session value with key "Test" should not be null
            Assert.NotNull(controller.HttpContext.Session.GetSession<ApplicationSettings>("Test"));
        }
    }
}