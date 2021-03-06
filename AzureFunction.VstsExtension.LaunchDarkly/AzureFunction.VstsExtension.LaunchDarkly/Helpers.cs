﻿using AzureFunction.VstsExtension.LaunchDarkly.AzureFunctions;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public class Helpers
    {
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public static int GetHeaderValue(HttpRequestMessage request, string name)
        {
            IEnumerable<string> values;
            var found = request.Headers.TryGetValues(name, out values);
            if (found)
            {
                return int.Parse(values.FirstOrDefault());
            }

            return 0;
        }

        public static string GetSecurityToken(AuthenticationHeaderValue value)
        {
            if (value?.Scheme != "Bearer")
            {
                return null;
            }
            else
            {
                return value.Parameter;
            }
        }

        public static string GetUserTokenInRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            //string apiVersion = Helpers.GetHeaderValue(request, "api-version");

            string issuedToken;

            issuedToken = Helpers.GetSecurityToken(request.Headers.Authorization);

            if (string.IsNullOrEmpty(issuedToken))
            {
                throw new SecurityTokenException();
            }
            else
            {
                return issuedToken;
            }
        }

        public static string GetExtCertificatEnvName(string appSettingExtCert, int apiversion, int minApiversion = 2)
        {
            if (apiversion >= minApiversion)
                return appSettingExtCert;
            else
                return "RollUpBoard_ExtensionCertificate";
        }

        public static string GetKeyVaultSecretValue(string ExtCertKey, TraceWriter log)
        {
            // This is the part where I grab the secret.
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            using (var keyClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
            {
                string keyVaultSecretUri = string.Format("https://{0}.vault.azure.net/secrets/{1}/", Helpers.GetEnvironmentVariable("KeyVaultName"), ExtCertKey);
                string secretvalue = keyClient.GetSecretAsync(keyVaultSecretUri).Result.Value;
                log.Info("Get the value from Key Vault: " + keyVaultSecretUri);
                return secretvalue;
            }
            
        }

        public static string TokenIsValid(HttpRequestMessage req, bool useKeyVault, string appSettingExtCert, string ExtCertKey, TraceWriter log)
        {
            string issuedToken = Helpers.GetUserTokenInRequest(req);

            var tokenuserId = string.Empty;
            if (useKeyVault)
            {
                string extCert = Helpers.GetKeyVaultSecretValue(ExtCertKey, log);
                tokenuserId = CheckVSTSToken.checkTokenValidityV2(issuedToken, extCert);
            }
            else
            {
                string extcert = appSettingExtCert;
                tokenuserId = CheckVSTSToken.checkTokenValidity(issuedToken, extcert);
            }

            return tokenuserId;
        }
    }
}