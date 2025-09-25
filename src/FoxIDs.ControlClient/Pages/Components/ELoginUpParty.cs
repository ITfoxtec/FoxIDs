using FoxIDs.Client.Models.ViewModels;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Client.Services;
using FoxIDs.Client.Logic;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using System.Net.Http;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ELoginUpParty : UpPartyBase
    {
        [Inject]
        public ClipboardLogic ClipboardLogic { get; set; }

        private bool showCssGenerator;
        private bool themeSectionExpanded = true;
        private bool backgroundSectionExpanded = false;
        private string themeColorHex = "#a4c700";
        private string themeBorderHex = "#454545";
        private double themeActiveDarkenPercent = 20;
        private string themeCssSnippet = string.Empty;
        private string themeCssError = string.Empty;

        private string backgroundColorHex = "#ddddc0";
        private string backgroundCssSnippet = string.Empty;
        private string backgroundCssError = string.Empty;


        private GeneralLoginUpPartyViewModel CurrentLoginUpParty => UpParty as GeneralLoginUpPartyViewModel;

        private bool HasThemeCss => !string.IsNullOrWhiteSpace(themeCssSnippet);
        private bool HasBackgroundCss => !string.IsNullOrWhiteSpace(backgroundCssSnippet);
        private string ThemeColorHexDisplay => TryParseHexColor(themeColorHex, out _, out _, out _, out var normalizedPrimaryHex) ? normalizedPrimaryHex : (string.IsNullOrWhiteSpace(themeColorHex) ? string.Empty : themeColorHex);
        private string BackgroundColorHexDisplay => TryParseHexColor(backgroundColorHex, out _, out _, out _, out var normalizedBackgroundHex) ? normalizedBackgroundHex : (string.IsNullOrWhiteSpace(backgroundColorHex) ? string.Empty : backgroundColorHex);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!UpParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalLoginUpParty = UpParty as GeneralLoginUpPartyViewModel;                
                var loginUpParty = await UpPartyService.GetLoginUpPartyAsync(UpParty.Name);
                await generalLoginUpParty.Form.InitAsync(ToViewModel(loginUpParty));
                if (generalLoginUpParty.ShowCreateUserTab && !loginUpParty.EnableCreateUser)
                {
                    generalLoginUpParty.ShowCreateUserTab = false;
                    generalLoginUpParty.ShowLoginTab = true;
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                UpParty.Error = ex.Message;
            }
        }

        private LoginUpPartyViewModel ToViewModel(LoginUpParty loginUpParty)
        {
            return loginUpParty.Map<LoginUpPartyViewModel>(afterMap: afterMap =>
            {
                afterMap.InitName = afterMap.Name;

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapOAuthClaimTransforms();
                }

                afterMap.Elements = afterMap.Elements.EnsureLoginDynamicDefaults().MapDynamicElementsAfterMap();
                afterMap.ExtendedUis.MapExtendedUis();

                if (afterMap.ExitClaimTransforms?.Count > 0)
                {
                    afterMap.ExitClaimTransforms = afterMap.ExitClaimTransforms.MapOAuthClaimTransforms();
                }
                if (afterMap.CreateUser?.ClaimTransforms?.Count > 0)
                {
                    afterMap.CreateUser.ClaimTransforms = afterMap.CreateUser.ClaimTransforms.MapOAuthClaimTransforms();
                }
            });
        }

        private async Task LoginUpPartyViewModelAfterInitAsync(GeneralLoginUpPartyViewModel loginParty, LoginUpPartyViewModel model)
        {
            if (loginParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync();
            }

            if (model.TwoFactorAppName.IsNullOrWhiteSpace())
            {
                model.TwoFactorAppName = TenantName;
            }
        }

        private async Task OnEditLoginUpPartyValidSubmitAsync(GeneralLoginUpPartyViewModel generalLoginUpParty, EditContext editContext)
        {
            try
            {
                generalLoginUpParty.Form.Model.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalLoginUpParty.Form.Model.Elements = generalLoginUpParty.Form.Model.Elements.EnsureLoginDynamicDefaults().MapDynamicElementsAfterMap();
                generalLoginUpParty.Form.Model.ExtendedUis.MapExtendedUisBeforeMap();
                generalLoginUpParty.Form.Model.ExitClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalLoginUpParty.Form.Model.CreateUser?.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                if (generalLoginUpParty.CreateMode)
                {
                    var loginUpPartyResult = await UpPartyService.CreateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
                        afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        afterMap.Elements.MapDynamicElementsAfterMap();
                        if (afterMap.CreateUser != null)
                        {
                            afterMap.CreateUser.Elements.MapDynamicElementsAfterMap();
                            afterMap.CreateUser.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        }
                        afterMap.ExtendedUis.MapExtendedUisAfterMap();
                        afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    }));
                    generalLoginUpParty.Form.UpdateModel(ToViewModel(loginUpPartyResult));
                    generalLoginUpParty.CreateMode = false;
                    toastService.ShowSuccess("Login application created.");
                    generalLoginUpParty.Name = loginUpPartyResult.Name;
                    generalLoginUpParty.DisplayName = loginUpPartyResult.DisplayName;
                }
                else
                {
                    var loginUpParty = await UpPartyService.UpdateLoginUpPartyAsync(generalLoginUpParty.Form.Model.Map<LoginUpParty>(afterMap: afterMap =>
                    {
                        if (generalLoginUpParty.Form.Model.Name != generalLoginUpParty.Form.Model.InitName)
                        {
                            afterMap.NewName = afterMap.Name;
                            afterMap.Name = generalLoginUpParty.Form.Model.InitName;
                        }

                        afterMap.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        afterMap.Elements.MapDynamicElementsAfterMap();
                        if (afterMap.CreateUser != null)
                        {
                            afterMap.CreateUser.Elements.MapDynamicElementsAfterMap();
                            afterMap.CreateUser.ClaimTransforms.MapOAuthClaimTransformsAfterMap();
                        }
                        afterMap.ExtendedUis.MapExtendedUisAfterMap();
                        afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    }));
                    generalLoginUpParty.Form.UpdateModel(ToViewModel(loginUpParty));
                    toastService.ShowSuccess("Login application updated.");
                    generalLoginUpParty.Name = loginUpParty.Name;
                    generalLoginUpParty.DisplayName = loginUpParty.DisplayName;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalLoginUpParty.Form.SetFieldError(nameof(generalLoginUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void OpenCssGenerator()
        {
            showCssGenerator = true;
            themeSectionExpanded = true;
            backgroundSectionExpanded = false;
            themeCssError = string.Empty;
            backgroundCssError = string.Empty;

            if (!HasThemeCss)
            {
                GenerateThemeCssSnippet();
            }

            if (!HasBackgroundCss)
            {
                GenerateBackgroundCssSnippet();
            }
        }

        private void CloseCssGenerator()
        {
            showCssGenerator = false;
        }

        private void ToggleThemeSection()
        {
            themeSectionExpanded = !themeSectionExpanded;
        }

        private void ToggleBackgroundSection()
        {
            backgroundSectionExpanded = !backgroundSectionExpanded;
        }

        private void GenerateThemeCssSnippet()
        {
            themeCssError = string.Empty;
            themeCssSnippet = string.Empty;

            if (!TryParseHexColor(themeColorHex, out var r, out var g, out var b, out var normalizedPrimaryHex))
            {
                themeCssError = "Enter a valid primary hex color in the format #RRGGBB.";
                return;
            }

            if (!TryParseHexColor(themeBorderHex, out var br, out var bg, out var bb, out var normalizedBorderHex))
            {
                themeCssError = "Enter a valid border hex color in the format #RRGGBB.";
                return;
            }

            if (themeActiveDarkenPercent < 0 || themeActiveDarkenPercent > 100)
            {
                themeCssError = "Active darken % must be between 0 and 100.";
                return;
            }

            themeColorHex = normalizedPrimaryHex;
            themeBorderHex = normalizedBorderHex;
            themeCssSnippet = CssThemeBuilder.GenerateThemeCss(r, g, b, normalizedBorderHex, themeActiveDarkenPercent);
        }

        private void GenerateBackgroundCssSnippet()
        {
            backgroundCssError = string.Empty;
            backgroundCssSnippet = string.Empty;

            if (!TryParseHexColor(backgroundColorHex, out var r, out var g, out var b, out var normalizedBackgroundHex))
            {
                backgroundCssError = "Enter a valid background hex color in the format #RRGGBB.";
                return;
            }

            backgroundColorHex = normalizedBackgroundHex;
            backgroundCssSnippet = $"body {{{Environment.NewLine}    background: {normalizedBackgroundHex};{Environment.NewLine}}}";
        }

        private void AppendThemeCssToModel()
        {
            AppendCssSnippet(themeCssSnippet);
        }

        private void AppendBackgroundCssToModel()
        {
            AppendCssSnippet(backgroundCssSnippet);
        }

        private async Task CopyThemeCssAsync()
        {
            if (!HasThemeCss || ClipboardLogic == null)
            {
                return;
            }

            await ClipboardLogic.WriteTextAsync(themeCssSnippet);
            toastService?.ShowSuccess("CSS snippet copied to the clipboard.");
        }

        private async Task CopyBackgroundCssAsync()
        {
            if (!HasBackgroundCss || ClipboardLogic == null)
            {
                return;
            }

            await ClipboardLogic.WriteTextAsync(backgroundCssSnippet);
            toastService?.ShowSuccess("CSS snippet copied to the clipboard.");
        }

        private void AppendCssSnippet(string snippet)
        {
            if (string.IsNullOrWhiteSpace(snippet))
            {
                return;
            }

            var login = CurrentLoginUpParty;
            if (login?.Form?.Model == null)
            {
                return;
            }

            if (login.Form.Model.Css.IsNullOrWhiteSpace())
            {
                login.Form.Model.Css = snippet.Trim();
            }
            else
            {
                var builder = new StringBuilder();
                builder.AppendLine(login.Form.Model.Css.TrimEnd());
                builder.AppendLine();
                builder.AppendLine(snippet.Trim());
                login.Form.Model.Css = builder.ToString();
            }

            var editContext = login.Form.EditContext;
            editContext?.NotifyFieldChanged(editContext.Field(nameof(LoginUpPartyViewModel.Css)));
        }

        private static bool TryParseHexColor(string rawHex, out int r, out int g, out int b, out string normalizedHex)
        {
            r = 0;
            g = 0;
            b = 0;
            normalizedHex = string.Empty;

            if (string.IsNullOrWhiteSpace(rawHex))
            {
                return false;
            }

            var hex = rawHex.Trim().TrimStart('#');

            if (hex.Length == 3)
            {
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
            }

            if (hex.Length != 6)
            {
                return false;
            }

            if (!int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) ||
                !int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) ||
                !int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            {
                return false;
            }

            r = ClampColor(r);
            g = ClampColor(g);
            b = ClampColor(b);
            normalizedHex = ToHex(r, g, b);
            return true;
        }

        private static int ClampColor(int value) => Math.Clamp(value, 0, 255);

        private static string ToHex(int r, int g, int b) => $"#{r:X2}{g:X2}{b:X2}";

        private static class CssThemeBuilder
        {
            public static string GenerateThemeCss(
                int r, int g, int b,
                string borderHex = "#454545",
                double activeDarkenPercent = 20.0)
            {
                Clamp(ref r, ref g, ref b);

                var primaryHex = ToHex(r, g, b);
                var (rd, gd, bd) = Darken(r, g, b, activeDarkenPercent);
                var activeHex = ToHex(rd, gd, bd);

                string focusRgba = $"rgba({r},{g},{b},.25)";

                return $@"
label {{
    color: {primaryHex} !important;
}}

.input:focus {{
    outline: none !important;
    border: 1px solid {primaryHex};
    box-shadow: 0 0 10px {primaryHex};
}}

.btn-link, .btn-link:hover, a, a:hover {{
    color: {primaryHex};
}}

.btn-primary.disabled, .btn-primary:disabled {{
    color: #fff;
    background-color: {primaryHex};
    border-color: {borderHex};
}}

.btn-primary,
.btn-primary:hover,
.btn-primary:active,
.btn-primary:focus {{
    background-color: {primaryHex};
    border-color: {borderHex};
}}

.btn-primary:not(:disabled):not(.disabled).active,
.btn-primary:not(:disabled):not(.disabled):active,
.show>.btn-primary.dropdown-toggle {{
    background-color: {activeHex};
    border-color: {borderHex};
}}

.btn-link:not(:disabled):not(.disabled):active,
.btn-link:not(:disabled):not(.disabled).active,
.show>.btn-link.dropdown-toggle {{
    color: {primaryHex};
}}

.btn:focus,
.form-control:focus {{
    border-color: {primaryHex};
    box-shadow: 0 0 0 .2rem {focusRgba};
}}

.btn-primary:not(:disabled):not(.disabled).active:focus,
.btn-primary:not(:disabled):not(.disabled):active:focus,
.show>.btn-primary.dropdown-toggle:focus {{
    box-shadow: 0 0 0 .2rem {focusRgba};
}}".Trim();
            }

            private static void Clamp(ref int r, ref int g, ref int b)
            {
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
            }

            private static string ToHex(int r, int g, int b)
                => $"#{r:X2}{g:X2}{b:X2}";

            private static (int r, int g, int b) Darken(int r, int g, int b, double percent)
            {
                double factor = Math.Clamp(1.0 - percent / 100.0, 0, 1);
                int rd = (int)Math.Round(r * factor);
                int gd = (int)Math.Round(g * factor);
                int bd = (int)Math.Round(b * factor);
                return (Math.Clamp(rd, 0, 255), Math.Clamp(gd, 0, 255), Math.Clamp(bd, 0, 255));
            }
        }

        private async Task DeleteLoginUpPartyAsync(GeneralLoginUpPartyViewModel generalLoginUpParty)
        {
            try
            {
                await UpPartyService.DeleteLoginUpPartyAsync(generalLoginUpParty.Name);
                UpParties.Remove(generalLoginUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalLoginUpParty.Form.SetError(ex.Message);
            }
        }
    }
}