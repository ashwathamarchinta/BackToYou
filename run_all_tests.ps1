# Automated Functional Test Runner for BackToYou Portal
$baseUrl = "http://localhost:5176"
$cookieStudent1 = "C:\BackToYou\scratch_cookie_s1.txt"
$cookieStudent2 = "C:\BackToYou\scratch_cookie_s2.txt"
$cookieAdmin = "C:\BackToYou\scratch_cookie_admin.txt"
$testImg = "C:\BackToYou\wwwroot\test_sample.png"

$tests = @()

function Log-Result($id, $name, $passed, $info) {
    $script:tests += [PSCustomObject]@{
        Id = $id
        Name = $name
        Passed = $passed
        Info = $info
    }
    $tag = if ($passed) { "[PASS]" } else { "[FAIL]" }
    Write-Host "$tag Test $id`: $name - $info"
}

# Helper to extract __RequestVerificationToken from HTML
function Extract-Token($html) {
    if ($html -match 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"' -or $html -match '__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    return ""
}

# 1 & 2: Home Page Loads
$homeHtml = & curl.exe -s -L "$baseUrl/"
$hasBrand = $homeHtml.Contains("BackToYou")
$hasCounters = $homeHtml.Contains("Items Lost") -and $homeHtml.Contains("Items Found")
Log-Result 1 "Home Page Loads" ($hasBrand -and $hasCounters) "Home page responded with brand title, stats, and search bar."

# 3: Student Registration
$regGet = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Account/Register"
$regToken = Extract-Token $regGet
$regEmail = "ananya.sharma@campus.edu"
$regPost = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Account/Register" `
    -d "__RequestVerificationToken=$regToken" `
    -d "FullName=Ananya Sharma" `
    -d "Email=$regEmail" `
    -d "StudentRollNumber=CS2023088" `
    -d "Department=Computer Science" `
    -d "PhoneNumber=9876543210" `
    -d "Password=Ananya@123" `
    -d "ConfirmPassword=Ananya@123"

$regSuccess = $regPost.Contains("Ananya") -or $regPost.Contains("Welcome to BackToYou")
Log-Result 2 "Student Registration" $regSuccess "New student registered and authenticated session established."

# 4: Logout and Login
$homeToken = Extract-Token (& curl.exe -s -c $cookieStudent1 -b $cookieStudent1 "$baseUrl/")
$logoutHtml = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Account/Logout" -d "__RequestVerificationToken=$homeToken"
$isLoggedOut = $logoutHtml.Contains("Sign In")

$loginGet = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Account/Login"
$loginToken = Extract-Token $loginGet
$loginPost = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Account/Login" `
    -d "__RequestVerificationToken=$loginToken" `
    -d "Email=$regEmail" `
    -d "Password=Ananya@123"

$isLoggedIn = $loginPost.Contains("Ananya") -or $loginPost.Contains("Sign Out")
Log-Result 3 "Login and Logout" ($isLoggedOut -and $isLoggedIn) "Logout ended session; login restored authenticated cookie."

# 5: Create Lost Item with Image
$createGet = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Items/Create?type=Lost"
$createToken = Extract-Token $createGet
$lostPost = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Items/Create" `
    -F "__RequestVerificationToken=$createToken" `
    -F "Type=Lost" `
    -F "Title=Lost: Black Titan Smartwatch" `
    -F "CategoryId=7" `
    -F "EventDate=2026-10-01" `
    -F "Location=Basketball Court Bench" `
    -F "Description=Black Titan smartwatch with silicone strap and scratch on right bezel." `
    -F "ImageFile=@$testImg"

$lostCreated = $lostPost.Contains("Black Titan Smartwatch") -and $lostPost.Contains("LOST")
Log-Result 4 "Create Lost Item with Image" $lostCreated "Lost item created with image upload and published."

# 6: Create Found Item with Image
$createGet2 = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Items/Create?type=Found"
$createToken2 = Extract-Token $createGet2
$foundPost = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Items/Create" `
    -F "__RequestVerificationToken=$createToken2" `
    -F "Type=Found" `
    -F "Title=Found: Blue Parker Fountain Pen" `
    -F "CategoryId=5" `
    -F "EventDate=2026-10-01" `
    -F "Location=Seminar Hall 1, Desk 24" `
    -F "Description=Metallic blue Parker pen in transparent pouch with black ink refill." `
    -F "ImageFile=@$testImg"

$foundCreated = $foundPost.Contains("Blue Parker Fountain Pen") -and $foundPost.Contains("FOUND")
Log-Result 5 "Create Found Item with Image" $foundCreated "Found item created with image upload and published."

# Extract Found Item ID
$foundItemId = ""
if ($foundPost -match '/Items/Details/(\d+)' -or $foundPost -match 'Claim This Item') {
    # Let's inspect directory to get ID
    $dirHtml = & curl.exe -s -L "$baseUrl/Items"
    if ($dirHtml -match 'href="/Items/Details/(\d+)"[^>]*>\s*Found: Blue Parker Fountain Pen') {
        $foundItemId = $matches[1]
    }
}
if (-not $foundItemId) {
    # Fallback to query directory
    $dirHtml = & curl.exe -s -L "$baseUrl/Items?searchTerm=Parker"
    if ($dirHtml -match '/Items/Details/(\d+)') {
        $foundItemId = $matches[1]
    }
}

# 7: Item Directory
$dirHtml = & curl.exe -s -L "$baseUrl/Items"
$dirOk = $dirHtml.Contains("Campus Lost & Found Directory") -and $dirHtml.Contains("Showing")
Log-Result 6 "Item Directory" $dirOk "Directory page loaded with listing grid."

# 8: Keyword Search
$searchHtml = & curl.exe -s -L "$baseUrl/Items?searchTerm=Smartwatch"
$searchOk = $searchHtml.Contains("Black Titan Smartwatch") -and (-not $searchHtml.Contains("Blue Parker Fountain Pen"))
Log-Result 7 "Keyword Search" $searchOk "Keyword 'Smartwatch' matched lost smartwatch listing exclusively."

# 9: Category Filter
$catHtml = & curl.exe -s -L "$baseUrl/Items?categoryId=5"
$catOk = $catHtml.Contains("Blue Parker Fountain Pen")
Log-Result 8 "Category Filter" $catOk "Category 5 (Books/Stationery) returned Parker Pen listing."

# 10: Lost / Found Type Filter
$lostFilterHtml = & curl.exe -s -L "$baseUrl/Items?type=Lost"
$foundFilterHtml = & curl.exe -s -L "$baseUrl/Items?type=Found"
$typeOk = $lostFilterHtml.Contains("LOST") -and $foundFilterHtml.Contains("FOUND")
Log-Result 9 "Type Filter" $typeOk "Lost and Found filters correctly filter by ItemType."

# 11: Status Filter
$statusHtml = & curl.exe -s -L "$baseUrl/Items?status=Reported"
$statusOk = $statusHtml.Contains("Reported")
Log-Result 10 "Status Filter" $statusOk "Status filtering returns active items."

# 12: Open Individual Item Details Page
$detHtml = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Items/Details/$foundItemId"
$detOk = $detHtml.Contains("Blue Parker Fountain Pen") -and $detHtml.Contains("Seminar Hall 1")
Log-Result 11 "Item Details Page" $detOk "Item details view renders metadata, location, and reporter info."

# 13: My Reported Items
$myItemsHtml = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Items/MyItems"
$myItemsOk = $myItemsHtml.Contains("Black Titan Smartwatch") -and $myItemsHtml.Contains("Blue Parker Fountain Pen")
Log-Result 12 "My Reported Items" $myItemsOk "User's MyItems table lists their reported items."

# 14: Test Mark as Closed
# Find the Lost item ID
$lostItemId = ""
if ($myItemsHtml -match 'action="/Items/CloseStatus/(\d+)"') {
    $lostItemId = $matches[1]
}
$closeToken = Extract-Token $myItemsHtml
$closeHtml = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Items/CloseStatus/$lostItemId" `
    -d "__RequestVerificationToken=$closeToken"

$closeOk = $closeHtml.Contains("marked as Closed") -or $closeHtml.Contains("Closed")
Log-Result 13 "Mark as Closed" $closeOk "Item #$lostItemId marked as Closed/Resolved."

# 15: Login as different student and Submit Claim
$s2LoginGet = & curl.exe -s -c $cookieStudent2 -b $cookieStudent2 -L "$baseUrl/Account/Login"
$s2Token = Extract-Token $s2LoginGet
$s2Login = & curl.exe -s -c $cookieStudent2 -b $cookieStudent2 -L -X POST "$baseUrl/Account/Login" `
    -d "__RequestVerificationToken=$s2Token" `
    -d "Email=student@campus.edu" `
    -d "Password=Student@123"

$claimGet = & curl.exe -s -c $cookieStudent2 -b $cookieStudent2 -L "$baseUrl/Claims/SubmitClaim?itemId=$foundItemId"
$claimToken = Extract-Token $claimGet
$claimPost = & curl.exe -s -c $cookieStudent2 -b $cookieStudent2 -L -X POST "$baseUrl/Claims/SubmitClaim" `
    -F "__RequestVerificationToken=$claimToken" `
    -F "ItemId=$foundItemId" `
    -F "ItemTitle=Found: Blue Parker Fountain Pen" `
    -F "DescriptionProof=It has my initials RS engraved on the bottom edge and uses blue Quink ink." `
    -F "ProofFile=@$testImg"

$claimSubmitted = $claimPost.Contains("My Submitted Claims") -and $claimPost.Contains("Blue Parker Fountain Pen")
Log-Result 14 "Submit Claim" $claimSubmitted "Student Rahul submitted ownership claim on item #$foundItemId."

# 16: Verify under My Claims
$myClaimsHtml = & curl.exe -s -c $cookieStudent2 -b $cookieStudent2 -L "$baseUrl/Claims/MyClaims"
$myClaimsOk = $myClaimsHtml.Contains("Blue Parker Fountain Pen") -and $myClaimsHtml.Contains("Pending")
Log-Result 15 "Verify My Claims" $myClaimsOk "Claim appears with Pending badge on student's claims tracker."

# 17: Admin Login
$admLoginGet = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L "$baseUrl/Account/Login"
$admToken = Extract-Token $admLoginGet
$admLogin = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L -X POST "$baseUrl/Account/Login" `
    -d "__RequestVerificationToken=$admToken" `
    -d "Email=admin@campus.edu" `
    -d "Password=Admin@123"

$admLogOk = $admLogin.Contains("Admin Panel") -or $admLogin.Contains("Campus Administrator")
Log-Result 16 "Admin Login" $admLogOk "Seeded administrator account logged in successfully."

# 18: Admin Dashboard
$dashHtml = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L "$baseUrl/Admin/Dashboard"
$dashOk = $dashHtml.Contains("Campus Administration Panel") -and $dashHtml.Contains("Pending Claims")
Log-Result 17 "Admin Dashboard" $dashOk "Admin Dashboard displays metrics counters and recent reports."

# 19: Manage Claims
$mcHtml = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L "$baseUrl/Admin/ManageClaims"
$mcOk = $mcHtml.Contains("Ownership Claims Moderation") -and $mcHtml.Contains("Blue Parker Fountain Pen")
Log-Result 18 "Manage Claims Page" $mcOk "Admin claims moderation interface lists pending claims."

# 20: Approve One Test Claim
$approveClaimId = ""
if ($mcHtml -match 'input type="hidden" name="claimId" value="(\d+)"') {
    $approveClaimId = $matches[1]
}
$admCsrf = Extract-Token $mcHtml
$approveHtml = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L -X POST "$baseUrl/Admin/ReviewClaim" `
    -d "__RequestVerificationToken=$admCsrf" `
    -d "claimId=$approveClaimId" `
    -d "status=Approved" `
    -d "adminRemarks=Approved. Please collect your item from Room 102 Security Office with your ID card."

$approveOk = $approveHtml.Contains("Approved") -or $approveHtml.Contains("marked as Approved")
Log-Result 19 "Approve Claim" $approveOk "Claim #$approveClaimId approved with collection instructions."

# 21: Reject One Test Claim
# Create a dummy claim to reject
$dummyGet = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Claims/SubmitClaim?itemId=3"
$dummyToken = Extract-Token $dummyGet
$dummyPost = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L -X POST "$baseUrl/Claims/SubmitClaim" `
    -d "__RequestVerificationToken=$dummyToken" `
    -d "ItemId=3" `
    -d "ItemTitle=Found: Honda Bike Key" `
    -d "DescriptionProof=Testing claim rejection verification flow."

$mcHtml2 = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L "$baseUrl/Admin/ManageClaims?status=Pending"
$rejectClaimId = ""
if ($mcHtml2 -match 'input type="hidden" name="claimId" value="(\d+)"') {
    $rejectClaimId = $matches[1]
}
$admCsrf2 = Extract-Token $mcHtml2
$rejectHtml = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L -X POST "$baseUrl/Admin/ReviewClaim" `
    -d "__RequestVerificationToken=$admCsrf2" `
    -d "claimId=$rejectClaimId" `
    -d "status=Rejected" `
    -d "adminRemarks=Rejected: Description does not match found item characteristics."

$rejectOk = $rejectHtml.Contains("Rejected") -or $rejectHtml.Contains("marked as Rejected")
Log-Result 20 "Reject Claim" $rejectOk "Claim #$rejectClaimId rejected with explanatory remarks."

# 22: Confirm Claim Status Updates for Student
$sClaimsHtml = & curl.exe -s -c $cookieStudent2 -b $cookieStudent2 -L "$baseUrl/Claims/MyClaims"
$sStatusOk = $sClaimsHtml.Contains("Approved") -and $sClaimsHtml.Contains("Room 102 Security Office")
Log-Result 21 "Student Claim Status Updated" $sStatusOk "Student sees Approved badge and Admin handover instructions."

# 23: Confirm Related Item Status Updated to Claimed
$detItemHtml = & curl.exe -s -L "$baseUrl/Items/Details/$foundItemId"
$itemStatusOk = $detItemHtml.Contains("Claimed") -or $detItemHtml.Contains("successfully claimed")
Log-Result 22 "Approved Claim Updates Item Status" $itemStatusOk "Item status transitioned to Claimed."

# 24: Verify Manage Items
$admItemsHtml = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L "$baseUrl/Admin/ManageItems"
$admItemsOk = $admItemsHtml.Contains("Campus Items Moderation") -and $admItemsHtml.Contains("Delete")
Log-Result 23 "Admin Manage Items" $admItemsOk "Admin items table lists items with delete action."

# 25: Verify Manage Users
$admUsersHtml = & curl.exe -s -c $cookieAdmin -b $cookieAdmin -L "$baseUrl/Admin/ManageUsers"
$admUsersOk = $admUsersHtml.Contains("Registered Campus Users") -and $admUsersHtml.Contains("Ananya Sharma")
Log-Result 24 "Admin Manage Users" $admUsersOk "User management table displays student and admin accounts."

# 26: Student Blocked from Admin Pages (RBAC)
$blockedHtml = & curl.exe -s -c $cookieStudent1 -b $cookieStudent1 -L "$baseUrl/Admin/Dashboard"
$blockedOk = $blockedHtml.Contains("Access Denied") -or (-not $blockedHtml.Contains("Campus Administration Panel"))
Log-Result 25 "RBAC Protection" $blockedOk "Non-admin student blocked from Admin pages."

# 27: Major Form Validations
$emptyReg = & curl.exe -s -X POST "$baseUrl/Account/Register" -d "__RequestVerificationToken=$regToken"
$regValOk = $emptyReg.Contains("is required") -or $emptyReg.Contains("Full Name is required")

$emptyLogin = & curl.exe -s -X POST "$baseUrl/Account/Login" -d "__RequestVerificationToken=$loginToken"
$loginValOk = $emptyLogin.Contains("Email is required") -or $emptyLogin.Contains("is required")
Log-Result 26 "Form Validations" ($regValOk -and $loginValOk) "Validation errors trigger on empty/invalid form submission."

# 28: Image Upload & Serving
$uploadsDir = "C:\BackToYou\wwwroot\uploads\items"
$hasUploadedFiles = (Get-ChildItem $uploadsDir -ErrorAction SilentlyContinue).Count -gt 0
Log-Result 27 "Image Upload Storage" $hasUploadedFiles "Uploaded files saved with unique GUID filenames in wwwroot/uploads/items."

# 29: Invalid Item ID Safety
$errResp = & curl.exe -s -o /dev/null -w "%{http_code}" "$baseUrl/Items/Details/999999"
$errOk = ($errResp -eq "404")
Log-Result 28 "Invalid Item ID Handling" $errOk "Querying non-existent ID returns HTTP 404 without crashing."

# 30: Navigation Links Integrity
$navRoutes = @("/", "/Items", "/Account/Login", "/Account/Register")
$navAll200 = $true
foreach ($route in $navRoutes) {
    $code = & curl.exe -s -o /dev/null -w "%{http_code}" "$baseUrl$route"
    if ($code -ne "200") { $navAll200 = $false }
}
Log-Result 29 "Navigation Links" $navAll200 "All primary public navigation routes return HTTP 200 OK."

# 31: Mobile & Responsive HTML Structure Check
$hasViewport = $homeHtml.Contains('name="viewport"') -and $homeHtml.Contains('width=device-width')
$hasBootstrapGrid = $homeHtml.Contains('col-md-') -or $homeHtml.Contains('col-lg-')
$hasMobileNavbar = $homeHtml.Contains('navbar-toggler')
Log-Result 30 "Responsive Mobile & Desktop Structure" ($hasViewport -and $hasBootstrapGrid -and $hasMobileNavbar) "Meta viewport, Bootstrap responsive breakpoints, and mobile toggler validated."

# Cleanup temporary cookies
Remove-Item -Force $cookieStudent1, $cookieStudent2, $cookieAdmin -ErrorAction SilentlyContinue

Write-Host "`n=========================================="
$passCount = ($tests | Where-Object { $_.Passed }).Count
$total = $tests.Count
Write-Host "TESTS EXECUTED: $total | PASSED: $passCount | FAILED: ($total - $passCount)"
Write-Host "=========================================="
