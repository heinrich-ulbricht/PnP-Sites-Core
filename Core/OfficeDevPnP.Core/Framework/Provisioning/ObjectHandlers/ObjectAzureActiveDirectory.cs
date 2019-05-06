﻿#if !ONPREMISES
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Diagnostics;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using System;
using System.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using OfficeDevPnP.Core.Framework.Provisioning.Model.Teams;
using OfficeDevPnP.Core.Utilities;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web;
using System.Net.Http;
using Microsoft.Online.SharePoint.TenantAdministration;
using OfficeDevPnP.Core.Framework.Provisioning.Model.AzureActiveDirectory;
using OfficeDevPnP.Core.Utilities.Graph;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    /// <summary>
    /// Object Handler to manage Microsoft AAD stuff
    /// </summary>
    internal class ObjectAzureActiveDirectory : ObjectHierarchyHandlerBase
    {
        public override string Name => "AzureActiveDirectory ";

        /// <summary>
        /// Creates a User in AAD and configures password and services
        /// </summary>
        /// <param name="scope">The PnP Provisioning Scope</param>
        /// <param name="parser">The PnP Token Parser</param>
        /// <param name="user">The User to create</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token</param>
        /// <returns></returns>
        private object CreateOrUpdateUser(PnPMonitoredScope scope, TokenParser parser, Model.AzureActiveDirectory.User user, string accessToken)
        {
            var content = PrepareUserRequestContent(user, parser);

            var userId = GraphHelper.CreateOrUpdateGraphObject(scope,
                HttpMethodVerb.POST,
                $"https://graph.microsoft.com/v1.0/users",
                content,
                HttpHelper.JsonContentType,
                accessToken,
                "ObjectConflict",
                CoreResources.Provisioning_ObjectHandlers_AAD_User_AlreadyExists,
                "userPrincipalName",
                user.UserPrincipalName,
                CoreResources.Provisioning_ObjectHandlers_AAD_User_ProvisioningError,
                canPatch: true);

            return (userId);
        }

        private object PrepareUserRequestContent(Model.AzureActiveDirectory.User user, TokenParser parser)
        {
            var content = new
            {
                accountEnabled = user.AccountEnabled,
                displayName = parser.ParseString(user.DisplayName),
                mailNickname = parser.ParseString(user.MailNickname),
                userPrincipalName = parser.ParseString(user.UserPrincipalName),
                givenName = parser.ParseString(user.GivenName),
                surname = parser.ParseString(user.Surname),
                jobTitle = parser.ParseString(user.JobTitle),
                mobilePhone = parser.ParseString(user.MobilePhone),
                officeLocation = parser.ParseString(user.OfficeLocation),
                preferredLanguage = parser.ParseString(user.PreferredLanguage),
                userType = "Member",
                passwordPolicies = parser.ParseString(user.PasswordPolicies),
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = user.PasswordProfile.ForceChangePasswordNextSignIn,
                    forceChangePasswordNextSignInWithMfa = user.PasswordProfile.ForceChangePasswordNextSignInWithMfa,
                    password = user.PasswordProfile.Password,
                }
            };

            return (content);
        }

        #region PnP Provisioning Engine infrastructural code

        public override bool WillProvision(Tenant tenant, ProvisioningHierarchy hierarchy, string sequenceId, ProvisioningTemplateApplyingInformation applyingInformation)
        {
#if !ONPREMISES
            if (!_willProvision.HasValue)
            {
                _willProvision = hierarchy.AzureActiveDirectory?.Users?.Any();
            }
#else
            if (!_willProvision.HasValue)
            {
                _willProvision = false;
            }
#endif
            return _willProvision.Value;
        }

        public override bool WillExtract(Tenant tenant, ProvisioningHierarchy hierarchy, string sequenceId, ProvisioningTemplateCreationInformation creationInfo)
        {
            if (!_willExtract.HasValue)
            {
                _willExtract = false;
            }
            return _willExtract.Value;
        }

        public override TokenParser ProvisionObjects(Tenant tenant, ProvisioningHierarchy hierarchy, string sequenceId, TokenParser parser, ProvisioningTemplateApplyingInformation applyingInformation)
        {
#if !ONPREMISES
            using (var scope = new PnPMonitoredScope(Name))
            {
                // Prepare a method global variable to store the Access Token
                String accessToken = null;

                // - Teams based on JSON templates
                var users = hierarchy.AzureActiveDirectory?.Users;
                if (users != null && users.Any())
                {
                    foreach (var u in users)
                    {
                        // Get a fresh Access Token for every request
                        accessToken = PnPProvisioningContext.Current.AcquireToken("https://graph.microsoft.com/", "User.ReadWrite.All");

                        // Creates or updates the User starting from the provisioning template definition
                        var userId = CreateOrUpdateUser(scope, parser, u, accessToken);

                        // If the user got created
                        if (userId != null)
                        {
                            // Manage the licensing settings

                        }
                    }
                }
            }
#endif

            return parser;
        }

        public override ProvisioningHierarchy ExtractObjects(Tenant tenant, ProvisioningHierarchy hierarchy, ProvisioningTemplateCreationInformation creationInfo)
        {
            // So far, no extraction
            return hierarchy;
        }

        #endregion
    }
}
#endif