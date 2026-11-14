param([string]$BaseUrl = 'http://127.0.0.1:5272', [switch]$MutateTestData, [switch]$IntegratedFlow)
$ErrorActionPreference = 'Stop'
$clients = [System.Collections.Generic.List[System.Net.Http.HttpClient]]::new()
$script:checks = 0
function Assert-Check($ok, [string]$label) {
    if (-not $ok) { throw "FAIL: $label" }
    $script:checks++
    Write-Output "PASS: $label"
}
function New-Client {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.BaseAddress = [uri]$BaseUrl
    $clients.Add($client)
    return $client
}
function Get-Response($client, [string]$path) { return $client.GetAsync($path).GetAwaiter().GetResult() }
function Post-Form($client, [string]$pagePath, [string]$postPath, [hashtable]$values) {
    $page = Get-Response $client $pagePath
    $html = $page.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $match = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw "No token at $pagePath" }
    $fields = [System.Collections.Generic.Dictionary[string,string]]::new()
    foreach ($key in $values.Keys) { $fields.Add($key, $values[$key]) }
    $fields.Add('__RequestVerificationToken', [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value))
    return $client.PostAsync($postPath,[System.Net.Http.FormUrlEncodedContent]::new($fields)).GetAwaiter().GetResult()
}
function Login($client, [string]$email) {
    $page = Get-Response $client '/Account/Login'
    $html = $page.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $match = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'Login form did not supply antiforgery token.' }
    $fields = [System.Collections.Generic.Dictionary[string,string]]::new()
    $fields.Add('email', $email)
    $fields.Add('matKhau', '123456')
    $fields.Add('__RequestVerificationToken', [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value))
    $response = $client.PostAsync('/Account/Login', [System.Net.Http.FormUrlEncodedContent]::new($fields)).GetAwaiter().GetResult()
    Assert-Check ([int]$response.StatusCode -eq 302) "login $email"
}
try {
    $anon = New-Client
    Assert-Check ([int](Get-Response $anon '/api/baohanh/BH20260012').StatusCode -eq 401) 'anonymous API returns 401'
    Assert-Check ([int](Get-Response $anon '/api/baohanh').StatusCode -eq 401) 'anonymous API list returns 401'
    Assert-Check ([int](Get-Response $anon '/Staff').StatusCode -eq 302) 'anonymous staff page requires login'
    $staff = New-Client
    Login $staff 'nhanvien@aurel.vn'
    foreach ($route in @('/Staff','/Staff/DonHang','/Staff/DonHang/ChiTiet/AM20260018','/Staff/DonHang/ChiTiet/AM20260012',
        '/Staff/DonHang/TaiQuay','/Staff/DonHang/TaiQuay?soDienThoai=0911222333','/Staff/LichHen',
        '/Staff/BaoHanh','/Staff/BaoHanh?maDon=AM20260012','/Staff/HoaDon/HD20260001')) {
        Assert-Check ([int](Get-Response $staff $route).StatusCode -eq 200) "staff page $route"
    }
    Assert-Check ([int](Get-Response $staff '/api/baohanh/BH20260012').StatusCode -eq 403) 'staff cannot access customer warranty API'
    $fields = [System.Collections.Generic.Dictionary[string,string]]::new()
    $fields.Add('maDon','AM20260018'); $fields.Add('lyDo','forged request')
    $bad = $staff.PostAsync('/Staff/DonHang/Huy',[System.Net.Http.FormUrlEncodedContent]::new($fields)).GetAwaiter().GetResult()
    Assert-Check ([int]$bad.StatusCode -eq 400) 'mutation without antiforgery token rejected'
    Assert-Check ([int](Get-Response $staff '/Staff/DonHang/HoanTat?maDon=AM20260018').StatusCode -eq 405) 'GET cannot complete an order'
    $owner = New-Client
    Login $owner 'khach1@gmail.com'
    $response = Get-Response $owner '/api/baohanh/BH20260012'
    Assert-Check ([int]$response.StatusCode -eq 200 -and $response.Content.Headers.ContentType.MediaType -eq 'application/xml') 'owner gets application/xml'
    [xml]$xml = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    Assert-Check ($xml.PhieuBaoHanh.MaPhieu -eq 'BH20260012' -and $xml.PhieuBaoHanh.Serial -eq 'DEMO012') 'XML response fields match contract'
    Assert-Check ([int](Get-Response $owner '/api/baohanh/BH99999999').StatusCode -eq 404) 'unknown warranty returns 404'
    Assert-Check ([int](Get-Response $owner '/Staff').StatusCode -eq 302) 'customer blocked from staff area'
    $other = New-Client
    Login $other 'khach2@gmail.com'
    Assert-Check ([int](Get-Response $other '/api/baohanh/BH20260012').StatusCode -eq 403) 'other customer receives 403'
    $response = Get-Response $other '/api/baohanh?maKH=KH001'
    [xml]$xml = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    Assert-Check ($xml.SelectNodes('/DanhSachPhieuBaoHanh/PhieuBaoHanh').Count -eq 0) 'query cannot override customer identity'
    $admin = New-Client
    Login $admin 'admin@aurel.vn'
    Assert-Check ([int](Get-Response $admin '/Staff/DonHang').StatusCode -eq 200) 'admin shares staff screens'
    Assert-Check ([int](Get-Response $admin '/api/baohanh/BH20260012').StatusCode -eq 403) 'admin cannot access customer API'
    if ($IntegratedFlow) {
        foreach ($route in @('/','/DanhMuc','/DanhMuc/ChiTiet/DH001','/DanhMuc/Xml','/Cart','/Account/Register')) {
            Assert-Check ([int](Get-Response $anon $route).StatusCode -eq 200) "public customer page $route"
        }
        foreach ($route in @('/Account','/Account?tab=lich-hen','/Booking','/Order/Tracking','/Warranty')) {
            Assert-Check ([int](Get-Response $owner $route).StatusCode -eq 200) "authenticated customer page $route"
        }
        foreach ($route in @('/Admin','/Admin/Kho','/Admin/DongHo','/Admin/TaiKhoan','/Admin/BaoCao','/Admin/DuLieu')) {
            Assert-Check ([int](Get-Response $admin $route).StatusCode -eq 200) "admin page $route"
        }
    }
    if ($MutateTestData) {
        # Chỉ dùng với website chạy trên bản sao dữ liệu kiểm thử.
        $detail = '/Staff/DonHang/ChiTiet/AM20260018'
        $response = Post-Form $staff $detail '/Staff/DonHang/XacNhan' @{maDon='AM20260018';serial='DEMO001';maNV='NV001'}
        Assert-Check ([int]$response.StatusCode -eq 302) 'confirm form redirects to detail'
        $html = (Get-Response $staff $detail).Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Assert-Check ($html.Contains('DEMO001') -and $html.Contains('NV002') -and $html.Contains('action="/Staff/DonHang/HoanTat"')) 'confirmation uses logged-in employee and updates buttons'
        $response = Post-Form $admin $detail '/Staff/DonHang/HoanTat' @{maDon='AM20260018';phuongThucTT='The';maNV='NV002'}
        Assert-Check ([int]$response.StatusCode -eq 302) 'complete form redirects to detail'
        $html = (Get-Response $admin $detail).Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $invoice = [regex]::Match($html, 'href="(/Staff/HoaDon/HD[0-9]+)"')
        Assert-Check ($invoice.Success) 'completed order links to invoice'
        $invoiceHtml = (Get-Response $admin $invoice.Groups[1].Value).Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Assert-Check ($invoiceHtml.Contains('NV001') -and $invoiceHtml.Contains('285,000,000')) 'invoice records actual admin and sale price'
        $response = Post-Form $staff '/Staff/BaoHanh?maDon=AM20260018' '/Staff/BaoHanh/LapPhieu' @{maDon='AM20260018';serial='DEMO001'}
        Assert-Check ([int]$response.StatusCode -eq 302) 'warranty form accepts completed order'
        $response = Get-Response $other '/api/baohanh'
        [xml]$xml = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $sold = $xml.SelectSingleNode('/DanhSachPhieuBaoHanh/PhieuBaoHanh[MaDon="AM20260018"]')
        Assert-Check ($null -ne $sold -and $sold.Serial -eq 'DEMO001') 'customer sees new warranty through real HTTP'
        Assert-Check ([int](Get-Response $owner ('/api/baohanh/'+$sold.MaPhieu)).StatusCode -eq 403) 'new warranty rejects different customer'
        if ($IntegratedFlow) {
            $response = Post-Form $owner '/DanhMuc/ChiTiet/DH001' '/Cart/Them' @{maMau='DH001';soLuong='1'}
            Assert-Check ([int]$response.StatusCode -eq 302) 'customer adds product to session cart'
            $response = Post-Form $owner '/Order/Checkout' '/Order/Checkout' @{TenNguoiNhan='Khách tích hợp';SoDienThoai='0901234567';HinhThucNhan='NhanTaiShowroom';Showroom='Showroom Đà Nẵng';MaKH='KH002';TongTien='1'}
            $location = [string]$response.Headers.Location
            Assert-Check ([int]$response.StatusCode -eq 302 -and $location -match '/Order/Success/(AM[0-9]+)') 'customer checkout creates real order'
            $orderId = $Matches[1]
            Assert-Check ([int](Get-Response $other ('/Order/ChiTiet/'+$orderId)).StatusCode -eq 404) 'checkout ignores forged customer identity'
            $html = (Get-Response $staff ('/Staff/DonHang/ChiTiet/'+$orderId)).Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Assert-Check ($html.Contains('285.000.000') -and $html.Contains('DEMO002')) 'staff sees customer order at server price'
            $response = Post-Form $staff ('/Staff/DonHang/ChiTiet/'+$orderId) '/Staff/DonHang/XacNhan' @{maDon=$orderId;serial='DEMO002'}
            Assert-Check ([int]$response.StatusCode -eq 302) 'staff confirms customer checkout order'
            $response = Post-Form $staff ('/Staff/DonHang/ChiTiet/'+$orderId) '/Staff/DonHang/HoanTat' @{maDon=$orderId;phuongThucTT='TienMat'}
            Assert-Check ([int]$response.StatusCode -eq 302) 'staff completes customer checkout order'
            $response = Post-Form $staff ('/Staff/BaoHanh?maDon='+$orderId) '/Staff/BaoHanh/LapPhieu' @{maDon=$orderId;serial='DEMO002'}
            [xml]$warranties = (Get-Response $owner '/api/baohanh').Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $warranty = $warranties.SelectSingleNode('/DanhSachPhieuBaoHanh/PhieuBaoHanh[Serial="DEMO002"]')
            Assert-Check ($null -ne $warranty -and $warranty.MaDon -eq $orderId) 'checkout owner receives warranty'
            $html = (Get-Response $owner ('/Warranty?maPhieu='+$warranty.MaPhieu)).Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Assert-Check ($html.Contains('DEMO002') -and $html.Contains('warranty-card')) 'customer HttpClient forwards cookie and renders warranty'
            $response = Post-Form $owner '/Booking' '/Booking' @{MaMau='DH003';Showroom='Showroom Đà Nẵng';NgayHen=(Get-Date).AddDays(2).Date.AddHours(10).ToString('yyyy-MM-ddTHH:mm:ss');HoTen='Khách tích hợp';SoDienThoai='0901234567';MaKH='KH002'}
            $location = [string]$response.Headers.Location
            Assert-Check ([int]$response.StatusCode -eq 302 -and $location -match '/Booking/Booked/(LH[0-9]+)') 'customer creates appointment'
            $bookingId = $Matches[1]
            Assert-Check ([int](Get-Response $other ('/Booking/Booked/'+$bookingId)).StatusCode -eq 404) 'appointment ignores forged customer identity'
            $response = Post-Form $staff '/Staff/LichHen' '/Staff/LichHen/XuLy' @{maLH=$bookingId;thaoTac='XacNhan'}
            Assert-Check ([int]$response.StatusCode -eq 302) 'staff confirms customer appointment'
            $html = [System.Net.WebUtility]::HtmlDecode((Get-Response $owner '/Account?tab=lich-hen').Content.ReadAsStringAsync().GetAwaiter().GetResult())
            Assert-Check ($html.Contains($bookingId) -and $html.Contains('Đã xác nhận')) 'customer account shows confirmed appointment'
        }
    }
    Write-Output "$script:checks HTTP checks passed. MutateTestData=$MutateTestData"
}
finally { foreach ($client in $clients) { $client.Dispose() } }
