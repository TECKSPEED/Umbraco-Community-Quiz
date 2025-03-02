﻿using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quiz.Site.Extensions;
using Quiz.Site.Filters;
using Quiz.Site.Models;
using Quiz.Site.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;
using Notification = Quiz.Site.Models.Notification;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Quiz.Site.Controllers.Surface
{
    public class AuthSurfaceController : SurfaceController
    {
        private readonly IMemberSignInManager _memberSignInManager;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;
        private readonly IAccountService _accountService;
        private readonly IBadgeService _badgeService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<AuthSurfaceController> _logger;

        public AuthSurfaceController(
            //these are required by the base controller
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            //these are dependencies we've added
            IMemberSignInManager memberSignInManager,
            IMemberManager memberManager,
            IMemberService memberService,
            IAccountService accountService,
            IBadgeService badgeService,
            INotificationRepository notificationRepository,
            ILogger<AuthSurfaceController> logger) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberSignInManager = memberSignInManager ?? throw new ArgumentNullException(nameof(memberSignInManager));
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _badgeService = badgeService ?? throw new ArgumentNullException(nameof(badgeService));
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ValidateCaptcha]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            SignInResult result = await _memberSignInManager.PasswordSignInAsync(
                model.Username, model.Password, isPersistent: model.RememberMe, lockoutOnFailure: true);

            var profilePage = CurrentPage.AncestorOrSelf<HomePage>().FirstChildOfType(ProfilePage.ModelTypeAlias);

            return RedirectToUmbracoPage(profilePage);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _memberSignInManager.SignOutAsync();

            return RedirectToCurrentUmbracoUrl();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var existingMember = _memberService.GetByEmail(model.Email);

            if (existingMember != null)
            {
                _logger.LogInformation("Register: Member has already been registered");
            }
            else
            {
                if (!ModelState.IsValid) return RedirectToCurrentUmbracoPage();


                var fullName = $"{model.Name} {model.Email}";

                var memberTypeAlias = CurrentPage.HasValue("memberType")
                    ? CurrentPage.Value<string>("memberType")
                    : Constants.Security.DefaultMemberTypeAlias;

                var identityUser = MemberIdentityUser.CreateNew(model.Email, model.Email, memberTypeAlias, isApproved: true, fullName);
                IdentityResult identityResult = await _memberManager.CreateAsync(
                    identityUser,
                    model.Password);

                var member = _memberService.GetByEmail(identityUser.Email);

                _logger.LogInformation("Register: Member created successfully");
                
                member.Name = model.Name;
                member.IsApproved = true;

                _memberService.Save(member);

                _memberService.AssignRoles(new[] { member.Username }, new[] { "Member" });
                
                if (DateTime.Now.Date < RegisterSurfaceController.EarlyAdopterThreshold.Date)
                {
                    if(member is not null)
                    {
                        AssignEarlyAdopterBadge(member);
                    }
                }
            }

            TempData["Success"] = true;
            return RedirectToCurrentUmbracoPage();
        }
        
        private void AssignEarlyAdopterBadge(IMember member)
        {
            var memberModel = _accountService.GetMemberModelFromMember(member);
            var earlyAdopterBadge = _badgeService.GetBadgeByName("Early Adopter");
            
            if(memberModel is not null && !_badgeService.HasBadge(memberModel, earlyAdopterBadge))
            {
                if(_badgeService.AddBadgeToMember(member, earlyAdopterBadge))
                {
                    _notificationRepository.Create(new Notification()
                    {
                        BadgeId = earlyAdopterBadge.GetUdiObject().ToString(),
                        MemberId = memberModel.Id,
                        Message = "New badge earned - " + earlyAdopterBadge.Name
                    });

                    TempData["ShowToast"] = true;
                }
            }
        }
    }
}
