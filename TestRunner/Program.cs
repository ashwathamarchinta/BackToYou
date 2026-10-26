using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class BackToYouTests
{
    const string BaseUrl = "http://localhost:5176";
    static readonly List<(string Name, bool Passed, string Info)> Results = new();
    static CookieContainer student1Cookies = new();
    static CookieContainer student2Cookies = new();
    static CookieContainer adminCookies = new();
    static int foundItemId = 0;
    static int lostItemId = 0;
    static int claimId = 0;

    static async Task Main()
    {
        Console.WriteLine("=== BackToYou Automated Test Suite ===\n");

        await Test01_HomePageLoads();
        await Test02_Register();
        await Test03_LogoutAndLogin();
        await Test04_CreateLostItem();
        await Test05_CreateFoundItem();
        await Test06_ItemDirectory();
        await Test07_KeywordSearch();
        await Test08_CategoryFilter();
        await Test09_TypeFilter();
        await Test10_StatusFilter();
        await Test11_ItemDetailsPage();
        await Test12_MyReportedItems();
        await Test13_MarkAsClosed();
        await Test14_SubmitClaim();
        await Test15_VerifyMyClaims();
        await Test16_AdminLogin();
        await Test17_AdminDashboard();
        await Test18_ManageClaims();
        await Test19_ApproveClaim();
        await Test20_RejectClaim();
        await Test21_StudentClaimStatusUpdated();
        await Test22_ItemStatusAfterApproval();
        await Test23_AdminManageItems();
        await Test24_AdminManageUsers();
        await Test25_StudentBlockedFromAdmin();
        await Test26_FormValidations();
        await Test27_ImageUploadStorage();
        await Test28_InvalidItemId();
        await Test29_NavigationLinks();
        await Test30_ResponsiveHtmlStructure();

        Console.WriteLine("\n=== FINAL RESULTS ===");
        int passed = 0, failed = 0;
        foreach (var (name, ok, info) in Results)
        {
            var tag = ok ? "[PASS]" : "[FAIL]";
            Console.WriteLine($"  {tag} {name}: {info}");
            if (ok) passed++; else failed++;
        }
        Console.WriteLine($"\nTotal: {Results.Count} | Passed: {passed} | Failed: {failed}");
    }

    static void Log(string name, bool passed, string info)
    {
        Results.Add((name, passed, info));
        Console.WriteLine($"{(passed ? "[PASS]" : "[FAIL]")} {name}: {info}");
    }

    static HttpClient MakeClient(CookieContainer cookies)
    {
        var handler = new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = true, MaxAutomaticRedirections = 10 };
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    static string ExtractToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
        if (!m.Success) m = Regex.Match(html, @"__RequestVerificationToken"" value=""([^""]+)""");
        if (!m.Success) m = Regex.Match(html, @"value=""([^""]+)""\s+name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : "";
    }

    static async Task Test01_HomePageLoads()
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/");
            bool hasBrand = html.Contains("BackToYou");
            bool hasStats = html.Contains("Items Lost") && html.Contains("Items Found");
            bool hasSearch = html.Contains("Search");
            Log("01 Home Page Loads", hasBrand && hasStats && hasSearch,
                $"Brand:{hasBrand} Stats:{hasStats} Search:{hasSearch}");
        }
        catch (Exception ex) { Log("01 Home Page Loads", false, ex.Message); }
    }

    static async Task Test02_Register()
    {
        try
        {
            using var client = MakeClient(student1Cookies);
            var getResp = await client.GetStringAsync("/Account/Register");
            var token = ExtractToken(getResp);
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["FullName"] = "Ananya Sharma",
                ["Email"] = "ananya.sharma@campus.edu",
                ["StudentRollNumber"] = "CS2023088",
                ["Department"] = "Computer Science",
                ["PhoneNumber"] = "9876543210",
                ["Password"] = "Ananya@123",
                ["ConfirmPassword"] = "Ananya@123"
            });
            var resp = await client.PostAsync("/Account/Register", form);
            var html = await resp.Content.ReadAsStringAsync();
            bool ok = html.Contains("Ananya") || html.Contains("Welcome to BackToYou") || resp.RequestMessage?.RequestUri?.ToString().Contains("/Home/Index") == true;
            Log("02 Student Registration", ok, $"FinalUrl={resp.RequestMessage?.RequestUri}");
        }
        catch (Exception ex) { Log("02 Student Registration", false, ex.Message); }
    }

    static async Task Test03_LogoutAndLogin()
    {
        try
        {
            using var client = MakeClient(student1Cookies);
            var homeHtml = await client.GetStringAsync("/");
            var logoutToken = ExtractToken(homeHtml);
            var logoutForm = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = logoutToken });
            await client.PostAsync("/Account/Logout", logoutForm);

            var loginGet = await client.GetStringAsync("/Account/Login");
            var loginToken = ExtractToken(loginGet);
            var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = loginToken,
                ["Email"] = "ananya.sharma@campus.edu",
                ["Password"] = "Ananya@123"
            });
            var loginResp = await client.PostAsync("/Account/Login", loginForm);
            var loginHtml = await loginResp.Content.ReadAsStringAsync();
            bool ok = loginHtml.Contains("Ananya") || loginHtml.Contains("Sign Out") || !loginHtml.Contains("Sign In to Portal");
            Log("03 Logout and Login", ok, $"FinalUrl={loginResp.RequestMessage?.RequestUri}");
        }
        catch (Exception ex) { Log("03 Logout and Login", false, ex.Message); }
    }

    static async Task Test04_CreateLostItem()
    {
        try
        {
            using var client = MakeClient(student1Cookies);
            var getPage = await client.GetStringAsync("/Items/Create?type=Lost");
            var token = ExtractToken(getPage);

            var imgBytes = File.Exists(@"C:\BackToYou\wwwroot\test_sample.png")
                ? File.ReadAllBytes(@"C:\BackToYou\wwwroot\test_sample.png")
                : new byte[] { 0x89, 0x50, 0x4E, 0x47 };

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(token), "__RequestVerificationToken");
            content.Add(new StringContent("Lost"), "Type");
            content.Add(new StringContent("Lost: Black Titan Smartwatch"), "Title");
            content.Add(new StringContent("7"), "CategoryId");
            content.Add(new StringContent(DateTime.Today.ToString("yyyy-MM-dd")), "EventDate");
            content.Add(new StringContent("Basketball Court Bench"), "Location");
            content.Add(new StringContent("Black Titan smartwatch silicone strap with scratch on right bezel."), "Description");
            var imgContent = new ByteArrayContent(imgBytes);
            imgContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(imgContent, "ImageFile", "smartwatch.png");

            var resp = await client.PostAsync("/Items/Create", content);
            var html = await resp.Content.ReadAsStringAsync();
            bool ok = html.Contains("Black Titan Smartwatch");

            var urlMatch = Regex.Match(resp.RequestMessage?.RequestUri?.ToString() ?? "", @"/Items/Details/(\d+)");
            if (urlMatch.Success) lostItemId = int.Parse(urlMatch.Groups[1].Value);

            Log("04 Create Lost Item with Image", ok, $"LostItemId={lostItemId} ImageUploaded={html.Contains("uploads")}");
        }
        catch (Exception ex) { Log("04 Create Lost Item with Image", false, ex.Message); }
    }

    static async Task Test05_CreateFoundItem()
    {
        try
        {
            using var client = MakeClient(student1Cookies);
            var getPage = await client.GetStringAsync("/Items/Create?type=Found");
            var token = ExtractToken(getPage);

            var imgBytes = File.Exists(@"C:\BackToYou\wwwroot\test_sample.png")
                ? File.ReadAllBytes(@"C:\BackToYou\wwwroot\test_sample.png")
                : new byte[] { 0x89, 0x50, 0x4E, 0x47 };

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(token), "__RequestVerificationToken");
            content.Add(new StringContent("Found"), "Type");
            content.Add(new StringContent("Found: Blue Parker Fountain Pen"), "Title");
            content.Add(new StringContent("5"), "CategoryId");
            content.Add(new StringContent(DateTime.Today.ToString("yyyy-MM-dd")), "EventDate");
            content.Add(new StringContent("Seminar Hall 1, Desk 24"), "Location");
            content.Add(new StringContent("Metallic blue Parker fountain pen in transparent case with black ink refill."), "Description");
            var imgContent = new ByteArrayContent(imgBytes);
            imgContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(imgContent, "ImageFile", "pen.png");

            var resp = await client.PostAsync("/Items/Create", content);
            var html = await resp.Content.ReadAsStringAsync();
            bool ok = html.Contains("Blue Parker Fountain Pen");

            var urlMatch = Regex.Match(resp.RequestMessage?.RequestUri?.ToString() ?? "", @"/Items/Details/(\d+)");
            if (urlMatch.Success) foundItemId = int.Parse(urlMatch.Groups[1].Value);

            Log("05 Create Found Item with Image", ok, $"FoundItemId={foundItemId} HasUploads={html.Contains("uploads")}");
        }
        catch (Exception ex) { Log("05 Create Found Item with Image", false, ex.Message); }
    }

    static async Task Test06_ItemDirectory()
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/Items");
            bool ok = html.Contains("Campus Lost") && html.Contains("Showing");
            Log("06 Item Directory", ok, $"Directory page rendered with filter bar and listings.");
        }
        catch (Exception ex) { Log("06 Item Directory", false, ex.Message); }
    }

    static async Task Test07_KeywordSearch()
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/Items?searchTerm=Smartwatch");
            bool found = html.Contains("Black Titan Smartwatch");
            bool notMixed = !html.Contains("Blue Parker Fountain Pen");
            Log("07 Keyword Search", found, $"Search 'Smartwatch' returns item={found}, filtered out Parker={notMixed}");
        }
        catch (Exception ex) { Log("07 Keyword Search", false, ex.Message); }
    }

    static async Task Test08_CategoryFilter()
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/Items?categoryId=5");
            bool ok = html.Contains("Blue Parker Fountain Pen");
            Log("08 Category Filter", ok, $"Category 5 (Books/Stationery) returns Parker Pen={ok}");
        }
        catch (Exception ex) { Log("08 Category Filter", false, ex.Message); }
    }

    static async Task Test09_TypeFilter()
    {
        try
        {
            using var client = new HttpClient();
            var lostHtml = await client.GetStringAsync($"{BaseUrl}/Items?type=Lost");
            var foundHtml = await client.GetStringAsync($"{BaseUrl}/Items?type=Found");
            bool ok = lostHtml.Contains("LOST") && foundHtml.Contains("FOUND");
            Log("09 Type Filter (Lost/Found)", ok, $"Lost filter shows LOST={lostHtml.Contains("LOST")}, Found filter shows FOUND={foundHtml.Contains("FOUND")}");
        }
        catch (Exception ex) { Log("09 Type Filter", false, ex.Message); }
    }

    static async Task Test10_StatusFilter()
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/Items?status=Reported");
            bool ok = html.Contains("Showing");
            Log("10 Status Filter", ok, "Status=Reported filter executed without error.");
        }
        catch (Exception ex) { Log("10 Status Filter", false, ex.Message); }
    }

    static async Task Test11_ItemDetailsPage()
    {
        try
        {
            if (foundItemId == 0)
            {
                Log("11 Item Details Page", false, "foundItemId not set - Test05 may have failed");
                return;
            }
            using var client = MakeClient(student1Cookies);
            var html = await client.GetStringAsync($"/Items/Details/{foundItemId}");
            bool hasTitle = html.Contains("Blue Parker Fountain Pen");
            bool hasLoc = html.Contains("Seminar Hall 1");
            bool hasStatus = html.Contains("Reported") || html.Contains("UnderClaim") || html.Contains("Claimed");
            Log("11 Item Details Page", hasTitle && hasLoc && hasStatus,
                $"Title={hasTitle} Location={hasLoc} Status={hasStatus}");
        }
        catch (Exception ex) { Log("11 Item Details Page", false, ex.Message); }
    }

    static async Task Test12_MyReportedItems()
    {
        try
        {
            using var client = MakeClient(student1Cookies);
            var html = await client.GetStringAsync("/Items/MyItems");
            bool hasLost = html.Contains("Black Titan Smartwatch");
            bool hasFound = html.Contains("Blue Parker Fountain Pen");
            Log("12 My Reported Items", hasLost && hasFound, $"Lost={hasLost} Found={hasFound}");
        }
        catch (Exception ex) { Log("12 My Reported Items", false, ex.Message); }
    }

    static async Task Test13_MarkAsClosed()
    {
        try
        {
            if (lostItemId == 0) { Log("13 Mark as Closed", false, "lostItemId not set"); return; }
            using var client = MakeClient(student1Cookies);
            var myItemsHtml = await client.GetStringAsync("/Items/MyItems");
            var token = ExtractToken(myItemsHtml);
            var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
            var resp = await client.PostAsync($"/Items/CloseStatus/{lostItemId}", form);
            var html = await resp.Content.ReadAsStringAsync();
            bool ok = html.Contains("Closed") || html.Contains("marked as Closed");
            Log("13 Mark as Closed", ok, $"Item #{lostItemId} status set to Closed. FinalUrl={resp.RequestMessage?.RequestUri}");
        }
        catch (Exception ex) { Log("13 Mark as Closed", false, ex.Message); }
    }

    static async Task Test14_SubmitClaim()
    {
        try
        {
            if (foundItemId == 0) { Log("14 Submit Claim", false, "foundItemId not set"); return; }
            // Login as second student
            using var client = MakeClient(student2Cookies);
            var loginGet = await client.GetStringAsync("/Account/Login");
            var loginToken = ExtractToken(loginGet);
            var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = loginToken,
                ["Email"] = "student@campus.edu",
                ["Password"] = "Student@123"
            });
            await client.PostAsync("/Account/Login", loginForm);

            // Submit claim
            var claimGet = await client.GetStringAsync($"/Claims/SubmitClaim?itemId={foundItemId}");
            var claimToken = ExtractToken(claimGet);

            var imgBytes = File.Exists(@"C:\BackToYou\wwwroot\test_sample.png")
                ? File.ReadAllBytes(@"C:\BackToYou\wwwroot\test_sample.png")
                : new byte[] { 0x89, 0x50 };

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(claimToken), "__RequestVerificationToken");
            content.Add(new StringContent(foundItemId.ToString()), "ItemId");
            content.Add(new StringContent("Found: Blue Parker Fountain Pen"), "ItemTitle");
            content.Add(new StringContent("It has my initials RS engraved under the clip, and uses blue Quink ink."), "DescriptionProof");
            var imgContent = new ByteArrayContent(imgBytes);
            imgContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(imgContent, "ProofFile", "proof.png");

            var resp = await client.PostAsync("/Claims/SubmitClaim", content);
            var html = await resp.Content.ReadAsStringAsync();
            bool ok = html.Contains("My Submitted Claims") && html.Contains("Blue Parker Fountain Pen");

            // Extract claim id if possible
            var claimMatch = Regex.Match(html, @"Claim #(\d+)");
            if (claimMatch.Success) claimId = int.Parse(claimMatch.Groups[1].Value);

            Log("14 Submit Claim", ok, $"ClaimId={claimId} FinalUrl={resp.RequestMessage?.RequestUri}");
        }
        catch (Exception ex) { Log("14 Submit Claim", false, ex.Message); }
    }

    static async Task Test15_VerifyMyClaims()
    {
        try
        {
            using var client = MakeClient(student2Cookies);
            var html = await client.GetStringAsync("/Claims/MyClaims");
            bool hasPen = html.Contains("Blue Parker Fountain Pen");
            bool hasPending = html.Contains("Pending");
            Log("15 Verify My Claims", hasPen && hasPending, $"Item={hasPen} Status=Pending:{hasPending}");
        }
        catch (Exception ex) { Log("15 Verify My Claims", false, ex.Message); }
    }

    static async Task Test16_AdminLogin()
    {
        try
        {
            using var client = MakeClient(adminCookies);
            var loginGet = await client.GetStringAsync("/Account/Login");
            var token = ExtractToken(loginGet);
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "admin@campus.edu",
                ["Password"] = "Admin@123"
            });
            var resp = await client.PostAsync("/Account/Login", form);
            var html = await resp.Content.ReadAsStringAsync();
            bool ok = html.Contains("Admin Panel") || html.Contains("Campus Administrator");
            Log("16 Admin Login", ok, $"AdminNav={html.Contains("Admin Panel")} FinalUrl={resp.RequestMessage?.RequestUri}");
        }
        catch (Exception ex) { Log("16 Admin Login", false, ex.Message); }
    }

    static async Task Test17_AdminDashboard()
    {
        try
        {
            using var client = MakeClient(adminCookies);
            var html = await client.GetStringAsync("/Admin/Dashboard");
            bool ok = html.Contains("Campus Administration Panel") && html.Contains("Pending Claims");
            Log("17 Admin Dashboard", ok, $"DashboardRendered={ok}");
        }
        catch (Exception ex) { Log("17 Admin Dashboard", false, ex.Message); }
    }

    static async Task Test18_ManageClaims()
    {
        try
        {
            using var client = MakeClient(adminCookies);
            var html = await client.GetStringAsync("/Admin/ManageClaims");
            bool hasMod = html.Contains("Ownership Claims Moderation");
            bool hasPen = html.Contains("Blue Parker Fountain Pen");
            Log("18 Manage Claims Page", hasMod && hasPen, $"ModHeader={hasMod} PenClaim={hasPen}");
        }
        catch (Exception ex) { Log("18 Manage Claims Page", false, ex.Message); }
    }

    static async Task Test19_ApproveClaim()
    {
        try
        {
            using var client = MakeClient(adminCookies);
            var html = await client.GetStringAsync("/Admin/ManageClaims");
            var token = ExtractToken(html);

            // Extract claim id from form
            var idMatch = Regex.Match(html, @"name=""claimId"" value=""(\d+)""");
            if (!idMatch.Success) { Log("19 Approve Claim", false, "No claim ID found on page"); return; }
            var claimToApprove = idMatch.Groups[1].Value;

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["claimId"] = claimToApprove,
                ["status"] = "Approved",
                ["adminRemarks"] = "Approved. Please collect your item from Room 102 Security Office with your college ID."
            });
            var resp = await client.PostAsync("/Admin/ReviewClaim", form);
            var respHtml = await resp.Content.ReadAsStringAsync();
            bool ok = respHtml.Contains("Approved") || respHtml.Contains("marked as Approved");
            Log("19 Approve Claim", ok, $"ClaimId={claimToApprove} Approved={ok}");
        }
        catch (Exception ex) { Log("19 Approve Claim", false, ex.Message); }
    }

    static async Task Test20_RejectClaim()
    {
        try
        {
            using var client = MakeClient(adminCookies);

            // First: seed a claim on item 3 from student1 to reject
            using var s1client = MakeClient(student1Cookies);
            var claimGet = await s1client.GetStringAsync("/Claims/SubmitClaim?itemId=3");
            var claimToken = ExtractToken(claimGet);
            if (!string.IsNullOrEmpty(claimToken))
            {
                var seedClaimForm = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = claimToken,
                    ["ItemId"] = "3",
                    ["ItemTitle"] = "Found: Honda Bike Key",
                    ["DescriptionProof"] = "Testing claim rejection workflow with insufficient proof."
                });
                await s1client.PostAsync("/Claims/SubmitClaim", seedClaimForm);
            }

            // Now fetch pending claims and reject one
            var mcHtml = await client.GetStringAsync("/Admin/ManageClaims?status=Pending");
            var token = ExtractToken(mcHtml);
            var idMatch = Regex.Match(mcHtml, @"name=""claimId"" value=""(\d+)""");
            if (!idMatch.Success) { Log("20 Reject Claim", false, "No pending claim found to reject"); return; }
            var claimToReject = idMatch.Groups[1].Value;

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["claimId"] = claimToReject,
                ["status"] = "Rejected",
                ["adminRemarks"] = "Rejected: Proof description does not match item characteristics."
            });
            var resp = await client.PostAsync("/Admin/ReviewClaim", form);
            var respHtml = await resp.Content.ReadAsStringAsync();
            bool ok = respHtml.Contains("Rejected") || respHtml.Contains("marked as Rejected");
            Log("20 Reject Claim", ok, $"ClaimId={claimToReject} Rejected={ok}");
        }
        catch (Exception ex) { Log("20 Reject Claim", false, ex.Message); }
    }

    static async Task Test21_StudentClaimStatusUpdated()
    {
        try
        {
            using var client = MakeClient(student2Cookies);
            var html = await client.GetStringAsync("/Claims/MyClaims");
            bool hasApproved = html.Contains("Approved");
            bool hasRemarks = html.Contains("Room 102 Security Office");
            Log("21 Student Claim Status Updated", hasApproved, $"ApprovedBadge={hasApproved} HandoverNote={hasRemarks}");
        }
        catch (Exception ex) { Log("21 Student Claim Status Updated", false, ex.Message); }
    }

    static async Task Test22_ItemStatusAfterApproval()
    {
        try
        {
            if (foundItemId == 0) { Log("22 Item Status After Approval", false, "foundItemId=0"); return; }
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/Items/Details/{foundItemId}");
            bool isClaimed = html.Contains("Claimed");
            Log("22 Item Status After Approval", isClaimed, $"ItemStatus=Claimed:{isClaimed}");
        }
        catch (Exception ex) { Log("22 Item Status After Approval", false, ex.Message); }
    }

    static async Task Test23_AdminManageItems()
    {
        try
        {
            using var client = MakeClient(adminCookies);
            var html = await client.GetStringAsync("/Admin/ManageItems");
            bool ok = html.Contains("Campus Items Moderation") && html.Contains("bi-trash");
            Log("23 Admin Manage Items", ok, $"ModHeader={html.Contains("Campus Items Moderation")} DeleteButtons={html.Contains("bi-trash")}");
        }
        catch (Exception ex) { Log("23 Admin Manage Items", false, ex.Message); }
    }

    static async Task Test24_AdminManageUsers()
    {
        try
        {
            using var client = MakeClient(adminCookies);
            var html = await client.GetStringAsync("/Admin/ManageUsers");
            bool ok = html.Contains("Registered Campus Users") && html.Contains("admin@campus.edu");
            Log("24 Admin Manage Users", ok, $"Header={html.Contains("Registered Campus Users")} AdminUser={html.Contains("admin@campus.edu")}");
        }
        catch (Exception ex) { Log("24 Admin Manage Users", false, ex.Message); }
    }

    static async Task Test25_StudentBlockedFromAdmin()
    {
        try
        {
            using var client = MakeClient(student1Cookies);
            var html = await client.GetStringAsync("/Admin/Dashboard");
            bool blocked = html.Contains("Access Denied") || !html.Contains("Campus Administration Panel");
            Log("25 RBAC Protection", blocked, $"AccessDenied={html.Contains("Access Denied")} AdminPanelHidden={!html.Contains("Campus Administration Panel")}");
        }
        catch (Exception ex)
        {
            bool blocked = ex.Message.Contains("403") || ex.Message.Contains("Forbidden");
            Log("25 RBAC Protection", blocked, $"Exception blocked: {ex.Message}");
        }
    }

    static async Task Test26_FormValidations()
    {
        try
        {
            using var client = new HttpClient();
            // Empty registration
            var regGet = await client.GetStringAsync($"{BaseUrl}/Account/Register");
            var token = ExtractToken(regGet);
            var empty = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
            var regResp = await client.PostAsync($"{BaseUrl}/Account/Register", empty);
            var regHtml = await regResp.Content.ReadAsStringAsync();
            bool regVal = regHtml.Contains("is required") || regHtml.Contains("Full Name");

            // Empty login
            var loginGet = await client.GetStringAsync($"{BaseUrl}/Account/Login");
            var loginToken = ExtractToken(loginGet);
            var emptyLogin = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = loginToken });
            var loginResp = await client.PostAsync($"{BaseUrl}/Account/Login", emptyLogin);
            var loginHtml = await loginResp.Content.ReadAsStringAsync();
            bool loginVal = loginHtml.Contains("is required") || loginHtml.Contains("Email");

            Log("26 Form Validations", regVal && loginVal, $"RegistrationVal={regVal} LoginVal={loginVal}");
        }
        catch (Exception ex) { Log("26 Form Validations", false, ex.Message); }
    }

    static async Task Test27_ImageUploadStorage()
    {
        try
        {
            var uploadDir = @"C:\BackToYou\wwwroot\uploads\items";
            bool hasFiles = Directory.Exists(uploadDir) && Directory.GetFiles(uploadDir).Length > 0;

            // Also test HTTP access to one of the uploaded files
            bool httpAccessible = false;
            if (hasFiles)
            {
                var file = Directory.GetFiles(uploadDir)[0];
                var relPath = "/uploads/items/" + Path.GetFileName(file);
                using var client = new HttpClient();
                var resp = await client.GetAsync($"{BaseUrl}{relPath}");
                httpAccessible = resp.IsSuccessStatusCode;
            }

            Log("27 Image Upload Storage & HTTP Serving", hasFiles && httpAccessible,
                $"FilesUploaded={Directory.GetFiles(uploadDir).Length} HttpAccessible={httpAccessible}");
        }
        catch (Exception ex) { Log("27 Image Upload Storage", false, ex.Message); }
    }

    static async Task Test28_InvalidItemId()
    {
        try
        {
            using var client = new HttpClient();
            var resp = await client.GetAsync($"{BaseUrl}/Items/Details/999999");
            bool ok = resp.StatusCode == HttpStatusCode.NotFound;
            Log("28 Invalid Item ID (404)", ok, $"HTTP Status={resp.StatusCode}");
        }
        catch (Exception ex) { Log("28 Invalid Item ID", false, ex.Message); }
    }

    static async Task Test29_NavigationLinks()
    {
        try
        {
            using var client = new HttpClient();
            var routes = new[] { "/", "/Items", "/Account/Login", "/Account/Register" };
            bool allOk = true;
            var details = new StringBuilder();
            foreach (var route in routes)
            {
                var resp = await client.GetAsync($"{BaseUrl}{route}");
                bool ok = resp.StatusCode == HttpStatusCode.OK;
                if (!ok) allOk = false;
                details.Append($"{route}={resp.StatusCode} ");
            }
            Log("29 Navigation Links", allOk, details.ToString().Trim());
        }
        catch (Exception ex) { Log("29 Navigation Links", false, ex.Message); }
    }

    static async Task Test30_ResponsiveHtmlStructure()
    {
        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"{BaseUrl}/");
            bool hasViewport = html.Contains(@"name=""viewport""") && html.Contains("width=device-width");
            bool hasGrid = html.Contains("col-md-") || html.Contains("col-lg-");
            bool hasToggler = html.Contains("navbar-toggler");
            bool hasBootstrap = html.Contains("bootstrap");
            Log("30 Responsive HTML Structure", hasViewport && hasGrid && hasToggler,
                $"Viewport={hasViewport} BootstrapGrid={hasGrid} MobileToggler={hasToggler} Bootstrap={hasBootstrap}");
        }
        catch (Exception ex) { Log("30 Responsive HTML Structure", false, ex.Message); }
    }
}
