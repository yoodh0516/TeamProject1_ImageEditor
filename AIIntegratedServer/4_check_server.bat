@echo off
setlocal EnableExtensions

powershell.exe -NoProfile -Command "try { $r = Invoke-RestMethod -Uri 'http://127.0.0.1:8000/health' -TimeoutSec 3; Write-Host 'Server connection OK:' $r.status; Write-Host 'Replicate configured:' $r.replicate_configured; Write-Host 'Gemini configured:' $r.gemini_configured; Write-Host 'Social post API:' ($r.features -contains 'social-post-recommendation'); Write-Host 'Face filter auto-fit API:' ($r.features -contains 'community-face-filter-auto-fit'); Start-Process 'http://127.0.0.1:8000/docs'; exit 0 } catch { Write-Host 'Cannot connect to the server. Run 3_start_server.bat first.'; exit 1 }"
echo.
pause
